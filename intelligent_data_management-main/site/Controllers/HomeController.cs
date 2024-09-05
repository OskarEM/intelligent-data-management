
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Site.Data;
    using Site.Models;

    namespace Site.Controllers
    {
        public class HomeController : Controller
        {
            private readonly ILogger<HomeController> _logger;
            private UserManager<IdentityUser> _um;
            private RoleManager<IdentityRole> _rm;
            private readonly ApplicationDbContext _dbContext;


            public HomeController(ILogger<HomeController> logger, UserManager<IdentityUser> um, RoleManager<IdentityRole> rm, ApplicationDbContext dbContext)
            {
                _logger = logger;
                _rm = rm;
                _um = um;
                _dbContext = dbContext;
            }

            public IActionResult Index()
            {
                return View();
            }

            public IActionResult Privacy()
            {
                return View();
            }
            
            
            
            
            
        }
    }
