using AspNetCore.ReCaptcha;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TheBlogProject.Data;
using TheBlogProject.Models;
using TheBlogProject.Services;
using TheBlogProject.ViewModels;
using X.PagedList;

namespace TheBlogProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IBlogEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IBlogEmailSender emailSender, ApplicationDbContext context)
        {
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
        }

        public async Task<IActionResult> Index(int? page)
        {
            var pageNumber = page ?? 1; // if page is null, 1
            var pageSize = 6;

            // display all blogs, in descending order based on the date created, with at least one post that is in production ready status,
            // with 5 blogs displayed per page.
            //var blogs = _context.Blogs.Where(
            //    b => b.Posts.Any(p => p.ReadyStatus == Enums.ReadyStatus.ProductionReady))
            //    .OrderByDescending(b => b.Created)
            //    .ToPagedListAsync(pageNumber, pageSize);

            // The above commented-out code would cause the application to throw an error if there was a blog without a bloguser.
            // This code below, specifically the .Include, fixes this by using "eager-loading".

            var blogs = _context.Blogs
            .Include(b => b.BlogUser)
            .OrderByDescending(b => b.Created)
            .ToPagedListAsync(pageNumber, pageSize);

            ViewData["HeaderImage"] = Url.Content("~/images/home-bg.jpg");
            ViewData["MainText"] = "My Blog App";
            ViewData["SubText"] = "Built using .NET 6 MVC & Bootstrap 5";

            return View(await blogs);
        }

        //public IActionResult About()
        //{
        //    return View();
        //}

        public IActionResult Contact()
        {
            return View();
        }

        [ValidateReCaptcha]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMe model)
        {
            // This is where we will be emailing...
            model.Message = $"{model.Message} <hr/> Phone: {model.Phone}";
            await _emailSender.SendContactEmailAsync(model.Email, model.Name, model.Subject, model.Message);
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}