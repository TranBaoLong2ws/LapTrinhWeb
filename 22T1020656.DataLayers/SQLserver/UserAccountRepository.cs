using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.HR;
using SV22T1020656.Models.Partner;
using SV22T1020656.Models.Security;

public class UserAccountRepository : IUserAccountRepository
{
    private readonly string _connectionString;

    public UserAccountRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // 1. Check Customer trước
        var customerSql = @"SELECT 
                                CAST(CustomerID AS NVARCHAR) AS UserId,
                                Email AS UserName,
                                CustomerName AS DisplayName,
                                Email,
                                NULL AS Photo,
                                N'customer' AS RoleNames
                            FROM Customers
                            WHERE Email = @userName 
                                  AND Password = @password
                                  AND (IsLocked = 0 OR IsLocked IS NULL)";

        var user = await connection.QueryFirstOrDefaultAsync<UserAccount>(customerSql, new
        {
            userName,
            password
        });

        if (user != null)
            return user;

        // 2. Nếu không có → check Employee
        var employeeSql = @"SELECT 
                                CAST(EmployeeID AS NVARCHAR) AS UserId,
                                Email AS UserName,
                                FullName AS DisplayName,
                                Email,
                                Photo,
                                RoleNames
                           FROM Employees
                           WHERE Email = @userName 
                                 AND Password = @password
                                 AND IsWorking = 1";

        return await connection.QueryFirstOrDefaultAsync<UserAccount>(employeeSql, new
        {
            userName,
            password
        });
    }

    public async Task<bool> ChangePasswordAsync(string userName, string password)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // update cả 2 bảng
        var sql = @"UPDATE Customers SET Password = @password WHERE Email = @userName;
                    UPDATE Employees SET Password = @password WHERE Email = @userName;";

        var rows = await connection.ExecuteAsync(sql, new { userName, password });
        return rows > 0;
    }

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        using var connection = new SqlConnection(_connectionString);

        var sql = @"SELECT COUNT(*) FROM Customers WHERE Email = @email";

        var count = await connection.ExecuteScalarAsync<int>(sql, new { email });

        return count > 0;
    }

    public async Task<bool> RegisterCustomerAsync(Customer customer)
    {
        using var connection = new SqlConnection(_connectionString);

        // Kiểm tra email đã tồn tại
        var checkSql = "SELECT COUNT(*) FROM Customers WHERE Email = @Email";
        var count = await connection.ExecuteScalarAsync<int>(checkSql, new { customer.Email });

        if (count > 0)
            return false;

        var sql = @"INSERT INTO Customers
                (CustomerName, ContactName, Email, Password, Address, Province, Phone, IsLocked)
                VALUES
                (@CustomerName, @ContactName, @Email, @Password, @Address, @Province, @Phone, 0)";

        var rows = await connection.ExecuteAsync(sql, customer);

        return rows > 0;
    }

    public async Task<bool> RegisterEmployeeAsync(Employee customer)
    {
        using var connection = new SqlConnection(_connectionString);

        // Kiểm tra email đã tồn tại
        var checkSql = "SELECT COUNT(*) FROM Employees WHERE Email = @Email";
        var count = await connection.ExecuteScalarAsync<int>(checkSql, new { customer.Email });

        if (count > 0)
            return false;
        var sql = @"INSERT INTO Employees
                (FullName, BirthDate, Address, Phone, Email, Password, Photo, IsWorking,RoleNames)
                VALUES
                (@FullName,@BirthDate,@Address,@Phone,@Email,@Password,@Photo,@IsWorking,@RoleNames)";

        var rows = await connection.ExecuteAsync(sql, customer);

        return rows > 0;
    }
}