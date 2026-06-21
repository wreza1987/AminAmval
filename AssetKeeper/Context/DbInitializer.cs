// using AssetKeeper.Domain.Entities;
// using AssetKeeper.Domain.Enums;

// namespace AssetKeeper.Context;

// public static class DbInitializer
// {
//     public static void Initialize(MyDbContext context)
//     {
//         // اگر قبلاً Seed شده، خارج شو
//         if (context.Categories.Any()) return;

//         // Seed Categories
//         var categories = new List<Category>
//         {
//             new Category { Name = "لپ‌تاپ", Description = "رایانه قابل حمل" },
//             new Category { Name = "موبایل", Description = "تلفن همراه" },
//             new Category { Name = "پرینتر", Description = "دستگاه چاپ" },
//             new Category { Name = "مانیتور", Description = "صفحه نمایش" },
//             new Category { Name = "مبلمان", Description = "تجهیزات اداری" }
//         };
//         context.Categories.AddRange(categories);
//         context.SaveChanges();

//         // Seed Brands
//         var brands = new List<Brand>
//         {
//             new Brand { Name = "Dell", Description = "دل" },
//             new Brand { Name = "HP", Description = "اچ‌پی" },
//             new Brand { Name = "Samsung", Description = "سامسونگ" },
//             new Brand { Name = "Iran", Description = "ایرانی" }
//         };
//         context.Brands.AddRange(brands);
//         context.SaveChanges();

//         // Seed Employees
//         var employees = new List<Employee>
//         {
//             new Employee { PersonnelCode = "P1001", FirstName = "علی", LastName = "احمدی", NationalCode = "1234567890", Department = "فناوری اطلاعات", StartDate = DateTime.Now.AddMonths(-12), AccessLevel = EmployeeAccessLevel.Admin },
//             new Employee { PersonnelCode = "P1002", FirstName = "مریم", LastName = "رضایی", NationalCode = "0987654321", Department = "مالی", StartDate = DateTime.Now.AddMonths(-8), AccessLevel = EmployeeAccessLevel.WarehouseKeeper },
//             new Employee { PersonnelCode = "P1003", FirstName = "حسن", LastName = "محمدی", NationalCode = "1122334455", Department = "اداری", StartDate = DateTime.Now.AddMonths(-6), AccessLevel = EmployeeAccessLevel.Normal }
//         };
//         context.Employees.AddRange(employees);
//         context.SaveChanges();

//         // Seed Assets
//         var assets = new List<Asset>
//         {
//             new Asset { AssetCode = "AM-00001", Name = "لپ‌تاپ دل", SerialNumber = "DL123456", CategoryId = categories[0].Id, BrandId = brands[0].Id, Status = AssetStatus.InStock, Owner = AssetOwner.Navaco },
//             new Asset { AssetCode = "AM-00002", Name = "موبایل سامسونگ", SerialNumber = "SM987654", CategoryId = categories[1].Id, BrandId = brands[2].Id, Status = AssetStatus.InStock, Owner = AssetOwner.MaskanBank }
//         };
//         context.Assets.AddRange(assets);
//         context.SaveChanges();
//     }
// }