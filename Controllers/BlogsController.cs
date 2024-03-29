﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TheBlogProject.Data;
using TheBlogProject.Models;
using TheBlogProject.Services;

namespace TheBlogProject.Controllers
{
    public class BlogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IImageService _imageService;
        private readonly UserManager<BlogUser> _userManager;
        private readonly IConfiguration _configuration;

        public BlogsController(ApplicationDbContext context, IImageService imageService, UserManager<BlogUser> userManager, IConfiguration configuration)
        {
            _context = context;
            _imageService = imageService;
            _userManager = userManager;
            _configuration = configuration;
        }

        // GET: Blogs
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Blogs.Include(b => b.BlogUser);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Blogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Blogs == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Include(b => b.BlogUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // GET: Blogs/Create
        [Authorize(Roles = "Administrator")]
        public IActionResult Create()
        {
            // Commented out line below because Id is not used directly on the form in Create.cshtml
            //ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Blogs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Image")] Blog blog)
        {
            if (ModelState.IsValid)
            {
                blog.Created = DateTime.Now;
                blog.BlogUserId = _userManager.GetUserId(User);

                // Use the _imageService to store the incoming user specified image or a default image, if null
                blog.ImageData = (await _imageService.EncodeImageAsync(blog.Image) ??
                    await _imageService.EncodeImageAsync(_configuration["DefaultBlogImage"]));

                blog.ContentType = _imageService.ContentType(blog.Image) ??
                    Path.GetExtension(_configuration["DefaultBlogImage"]);

                _context.Add(blog);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", blog.BlogUserId);
            return View(blog);
        }

        // GET: Blogs/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Blogs == null)
            {
                return NotFound();
            }

            var authorId = _userManager.GetUserId(User);
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id && b.BlogUserId == authorId);

            if (blog == null)
            {
                return NotFound();
            }
            //ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", blog.BlogUserId);
            return View(blog);
        }

        // POST: Blogs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Blog blog, IFormFile? newImage)
        {
            if (id != blog.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalBlog = await _context.Blogs.FindAsync(blog.Id);

                    originalBlog.Updated = DateTime.Now;
                    
                    if(originalBlog.Name != blog.Name)
                    {
                        originalBlog.Name = blog.Name;
                    }

                    if(originalBlog.Description != blog.Description)
                    {
                        originalBlog.Description = blog.Description;
                    }

                    if(newImage is not null)
                    {
                        originalBlog.ImageData = await _imageService.EncodeImageAsync(newImage);
                        originalBlog.ContentType = _imageService.ContentType(newImage);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogExists(blog.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
                //return RedirectToAction("Index", "Home");
            }

            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", blog.BlogUserId);
            return View(blog);
        }

        // GET: Blogs/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Blogs == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .Include(b => b.BlogUser)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        // POST: Blogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var authorId = _userManager.GetUserId(User);
            var blog = await _context.Blogs.FirstOrDefaultAsync(b => b.Id == id && b.BlogUserId == authorId);

            if (blog != null)
            {
                _context.Blogs.Remove(blog);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool BlogExists(int id)
        {
          return (_context.Blogs?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
