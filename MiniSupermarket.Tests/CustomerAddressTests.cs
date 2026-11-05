using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using MiniSupermarket.API.Models;
using Xunit;

namespace MiniSupermarket.Tests
{
    public class CustomerAddressTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Quận 1, TP.HCM")]
        [InlineData("12, Nguyen Hue, Thành phố Hồ Chí Minh")]
        [InlineData("12, , Phường Sài Gòn, Thành phố Hồ Chí Minh")]
        [InlineData("12, Nguyen Hue, Quận 1, Thành phố Hồ Chí Minh")]
        [InlineData("12, Nguyen Hue, Phường Sài Gòn, Thành phố Hồ Chí Minh, Extra")]
        [InlineData("12, Nguyen Hue, Phường Sài Gòn, Hồ Chí Minh")]
        public async Task InvalidAddressesAreRejectedOnCreateAndUpdate(string? address)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("admin01");
            var request = new { CustomerName = "Address test", PhoneNumber = "0909999998", Address = address };
            var original = await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.Address).SingleAsync());
            using var created = await client.PostAsJsonAsync("/api/customers", request);
            using var updated = await client.PutAsJsonAsync("/api/customers/1", request);
            Assert.Equal(HttpStatusCode.BadRequest, created.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, updated.StatusCode);
            Assert.Equal(20, await app.QueryAsync(db => db.Customers.CountAsync()));
            Assert.Equal(original, await app.QueryAsync(db => db.Customers.Where(c => c.CustomerId == 1).Select(c => c.Address).SingleAsync()));
        }

        [Theory]
        [InlineData("12, Đường Nguyễn Huệ, Phường Sài Gòn, Thành phố Hồ Chí Minh")]
        [InlineData("56/2A, Đường số 1, Xã Bình Khánh, Thành phố Hồ Chí Minh")]
        [InlineData("90B, Đường số 2, Xã Bình Sơn, Tỉnh Đồng Nai")]
        [InlineData("12, Nguyen Hue, PHƯỜNG SÀI GÒN, TP. Hồ Chí Minh")]
        public async Task FullWardCommuneProvinceAndCityAddressesAreAccepted(string address)
        {
            using var app = new ApiFactory();
            using var client = await app.LoginAsync("admin01");
            var request = new CustomerRequest { CustomerName = "Address test", PhoneNumber = "0909999998", Address = address };
            using var created = await client.PostAsJsonAsync("/api/customers", request);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var customer = await created.Content.ReadFromJsonAsync<CustomerDto>();
            Assert.Equal(address, customer!.Address);
            using var updated = await client.PutAsJsonAsync($"/api/customers/{customer.CustomerId}", request);
            Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);
        }

        [Fact]
        public async Task SqlAndEfCustomerSeedsHaveTheSameTwentyCompleteAddresses()
        {
            using var app = new ApiFactory();
            var customers = await app.QueryAsync(db => db.Customers.AsNoTracking().OrderBy(c => c.CustomerId).ToListAsync());
            Assert.Equal(20, customers.Count);
            using var reader = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Database", "MinhKhangMiniSupermarketDb.sql"));
            var fragment = new TSql160Parser(true).Parse(reader, out var errors);
            Assert.Empty(errors);
            var visitor = new CustomerSeedVisitor();
            fragment.Accept(visitor);
            Assert.Equal(20, visitor.Rows.Count);
            foreach (var customer in customers)
            {
                var row = visitor.Rows[customer.CustomerId];
                Assert.Equal(customer.CustomerName, row.Name);
                Assert.Equal(customer.PhoneNumber, row.Phone);
                Assert.Equal(customer.Address, row.Address);
                Assert.Equal(4, customer.Address.Split(',').Length);
                Assert.True(Validator.TryValidateObject(customer, new ValidationContext(customer), new List<ValidationResult>(), true));
            }
            Assert.Equal(4, visitor.ProcedureCount);
        }

        [Fact]
        public async Task DatabaseRejectsAnAddressWithMissingParts()
        {
            using var app = new ApiFactory();
            await Assert.ThrowsAsync<DbUpdateException>(() => app.QueryAsync(async db =>
            {
                var customer = await db.Customers.FirstAsync();
                customer.Address = "Old district-only address";
                return await db.SaveChangesAsync();
            }));
        }

        private sealed class CustomerSeedVisitor : TSqlFragmentVisitor
        {
            public Dictionary<int, (string Name, string Phone, string Address)> Rows { get; } = new();
            public int ProcedureCount { get; private set; }

            public override void ExplicitVisit(InsertStatement node)
            {
                var insert = node.InsertSpecification;
                if (insert.Target is not NamedTableReference table || table.SchemaObject.BaseIdentifier.Value != "Customers") return;
                foreach (var row in ((ValuesInsertSource)insert.InsertSource).RowValues)
                {
                    var id = int.Parse(((IntegerLiteral)row.ColumnValues[0]).Value);
                    Rows.Add(id, (((StringLiteral)row.ColumnValues[1]).Value,
                        ((StringLiteral)row.ColumnValues[2]).Value, ((StringLiteral)row.ColumnValues[3]).Value));
                }
            }

            public override void ExplicitVisit(CreateOrAlterProcedureStatement node) => ProcedureCount++;
        }
    }
}
