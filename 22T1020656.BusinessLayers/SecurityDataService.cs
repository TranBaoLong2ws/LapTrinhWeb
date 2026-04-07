using SV22T1020656.Models.Security;
using SV22T1020656.DataLayers.Interfaces;
using SV22T1020656.Models.Partner;
using System.Threading.Tasks;
using SV22T1020656.Models.HR;



namespace SV22T1020656.BusinessLayers
{
    public static class SecurityDataService
    {
        private static IUserAccountRepository userAccountDb;

        static SecurityDataService()
        {
            userAccountDb = new UserAccountRepository(Configuration.ConnectionString);
        }

        public static async Task<UserAccount> LoginAsync(String userName, String passWord)
        {
            return await userAccountDb.AuthorizeAsync(userName, passWord);
        }

        public static async Task<bool> RegisterAsync(registerModel model)
        {
            //if (model.Password != model.RePassword)
            //    return false;

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                ContactName = model.CustomerName, 
                Email = model.Email,
                Password = model.Password,
                Address = "",
                Province = "",
                Phone = ""
            };

            return await userAccountDb.RegisterCustomerAsync(customer);
        }

        public static async Task<bool> RegisterEmployee(registerModel model)
        {
            var employee = new Employee
            {
                FullName = model.CustomerName, // Lưu ý: Nên đổi tên field trong model thành FullName cho đồng nhất
                BirthDate = new DateTime(2000, 1, 1), // Hoặc để DateTime.Now tùy bạn
                Address = "",
                Phone = "",
                Email = model.Email,
                Password = model.Password,
                Photo = "",
                IsWorking = true,        // QUAN TRỌNG: Phải có cái này mới qua được bộ lọc đăng nhập
                RoleNames = "employee"
            };
            return await userAccountDb.RegisterEmployeeAsync(employee);
        }


        //public static async Task<bool> RegisterAsync(UserAccount data, string password)
        //{
        //    return await userAccountDb.Re(data, password);
        //}

        public static async Task<bool> ChangePasswordAsync(String username, String password)
        {
            return await userAccountDb.ChangePasswordAsync(username,password);
        }


        public static async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await userAccountDb.IsEmailExistsAsync(email);
        }

    }
}
