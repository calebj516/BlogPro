﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBlogProject.Data;
using TheBlogProject.Models;
using TheBlogProject.Services;
using TheBlogProject.Enums;
using X.PagedList;
using TheBlogProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Drawing.Printing;
using MailKit.Search;
using Microsoft.Extensions.Hosting;

namespace TheBlogProject.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISlugService _slugService;
        private readonly IImageService _imageService;
        private readonly UserManager<BlogUser> _userManager;
        private readonly BlogSearchService _blogSearchService;
        private readonly IConfiguration _configuration;

        public PostsController(ApplicationDbContext context, ISlugService slugService, IImageService imageService, UserManager<BlogUser> userManager, BlogSearchService blogSearchService, IConfiguration configuration)
        {
            _context = context;
            _slugService = slugService;
            _imageService = imageService;
            _userManager = userManager;
            _blogSearchService = blogSearchService;
            _configuration = configuration;
        }

        public async Task<IActionResult> SearchIndex(int? page, string searchTerm)
        {
            ViewData["SearchTerm"] = searchTerm;

            var pageNumber = page ?? 1;
            var pageSize = 6;

            var posts = _blogSearchService.Search(searchTerm);
            return View(await posts.ToPagedListAsync(pageNumber, pageSize));
        }

        // GET: Posts
        [Authorize]
        public async Task<IActionResult> Index(int? page)
        {
            //var applicationDbContext = _context.Posts.Include(p => p.Blog).Include(p => p.BlogUser);
            //return View(await applicationDbContext.ToListAsync());

            var pageNumber = page ?? 1; // if page is null, 1
            var pageSize = 6;

            var posts = _context.Posts.Include(p => p.Blog)
            .Include(p => p.BlogUser)
            .OrderByDescending(b => b.Created)
            .ToPagedListAsync(pageNumber, pageSize);

            ViewData["MainText"] = "Post Index";

            return View(await posts);
        }

        // GET: Posts that have a matching tag
        public async Task<IActionResult> TagIndex(string tag, int? page)
        {
            //var applicationDbContext = _context.Posts
            //    .Where(p => p.Tags.Any(t => t.Text.ToLower() == tag.ToLower()));

            //return View(await applicationDbContext.ToListAsync());

            var pageNumber = page ?? 1;
            var pageSize = 6;

            var posts = await _context.Posts
                .Where(p => p.Tags.Any(t => t.Text.ToLower() == tag.ToLower()))
                .OrderByDescending(p => p.Created)
                .ToPagedListAsync(pageNumber, pageSize);

            ViewData["MainText"] = $"All Posts that Include the Tag \"{ tag }\"";
            ViewData["tag"] = tag;

            return View(posts);
        }

        // GET: Posts/Details/5
        //public async Task<IActionResult> Details(int? id)
        //{
        //    if (id == null || _context.Posts == null)
        //    {
        //        return NotFound();
        //    }

        //    var post = await _context.Posts
        //        .Include(p => p.Blog)
        //        .Include(p => p.BlogUser)
        //        .Include(p => p.Tags)
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (post == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(post);
        //}

        public async Task<IActionResult> BlogPostIndex(int? id, int? page)
        {
            if(id is null)
            {
                return NotFound();
            }

            var pageNumber = page ?? 1;
            var pageSize = 6;

            var posts = await _context.Posts
                .Where(p => p.BlogId == id && p.ReadyStatus == ReadyStatus.ProductionReady)
                .OrderByDescending(p => p.Created)
                .ToPagedListAsync(pageNumber, pageSize);

            return View(posts);
        }

        //public async Task<IActionResult> Details(string slug)
        //{
        //    if (string.IsNullOrEmpty(slug))
        //    {
        //        return NotFound();
        //    }

        //    var post = await _context.Posts
        //        .Include(p => p.Blog)
        //        .Include(p => p.BlogUser)
        //        .Include(p => p.Tags)
        //        .Include(p => p.Comments)
        //        .ThenInclude(c => c.BlogUser)
        //        .FirstOrDefaultAsync(m => m.Slug == slug);

        //    if (post == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(post);
        //}

        // Updated Details View to use PostDetailViewModel
        public async Task<IActionResult> Details(string slug)
        {
            ViewData["Title"] = "Post Details Page";
            if (string.IsNullOrEmpty(slug)) return NotFound();

            var post = await _context.Posts
                .Include(p => p.Blog)
                .Include(p => p.BlogUser)
                .Include(p => p.Tags)
                .Include(p => p.Comments)
                .ThenInclude(c => c.BlogUser)
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (post == null) return NotFound();

            var dataVM = new PostDetailViewModel()
            {
                Post = post,
                Tags = _context.Tags
                        .Select(t => t.Text.ToLower())
                        .Distinct().ToList()
            };

            //ViewData["HeaderImage"] = _imageService.DecodeImage(post.ImageData, post.ContentType);
            if (post.ImageData != null)
            {
                ViewData["HeaderImage"] = _imageService.DecodeImage(post.ImageData, post.ContentType);
            }

            ViewData["MainText"] = post.Title;
            ViewData["SubText"] = post.Abstract;

            return View(dataVM);
        }

        // GET: Posts/Create
        [Authorize]
        public IActionResult Create()
        {
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name");
            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("BlogId,Title,Abstract,Content,ReadyStatus,Image")] Post post, List<string> tagValues)
        {
            if (ModelState.IsValid)
            {
                post.Created = DateTime.Now;

                var authorId = _userManager.GetUserId(User);
                post.BlogUserId = authorId;

                // Use the _imageService to store the incoming user specified image or a default image, if null
                post.ImageData = (await _imageService.EncodeImageAsync(post.Image) ??
                    await _imageService.EncodeImageAsync(_configuration["DefaultPostImage"]));

                post.ContentType = _imageService.ContentType(post.Image) ??
                    Path.GetExtension(_configuration["DefaultPostImage"]);


                // Create the slug and determine if it is unique
                var slug = _slugService.UrlFriendly(post.Title);

                // Create a variable to store whether an error has occurred
                var validationError = false;

                if (string.IsNullOrEmpty(slug))
                {
                    validationError = true;
                    ModelState.AddModelError("", "The Title you provided cannot be used due to it being empty.");
                }

                // Detect incoming duplicate Slugs
                else if (!_slugService.IsUnique(slug))
                {
                    validationError = true;
                    ModelState.AddModelError("Title", "The Title you provided cannot be used due to it resulting in a duplicate value.");
                }

                else if (slug.Contains("test") && slug.Length == 4)
                {
                    validationError = true;
                    ModelState.AddModelError("", "Uh-oh! Are you testing again?");
                    ModelState.AddModelError("Title", "The Title cannot contain the word 'test'.");
                }

                if (validationError)
                {
                    ViewData["TagValues"] = string.Join(",", tagValues);
                    return View(post);
                }

                post.Slug = slug;

                _context.Add(post);
                await _context.SaveChangesAsync();

                foreach(var tagText in tagValues)
                {
                    _context.Add(new Tag()
                    {
                        PostId = post.Id,
                        BlogUserId = authorId,
                        Text = tagText
                    });
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Description", post.BlogId);

            return View(post);
        }

        // GET: Posts/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null || _context.Posts == null)
        //    {
        //        return NotFound();
        //    }

        //    var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == id);

        //    if (post == null)
        //    {
        //        return NotFound();
        //    }
        //    ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name", post.BlogId);
        //    ViewData["TagValues"] = string.Join(",", post.Tags.Select(t => t.Text));

        //    return View(post);
        //}

        [Authorize]
        public async Task<IActionResult> Edit(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var authorId = _userManager.GetUserId(User);
            var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Slug == slug && p.BlogUserId == authorId);

            if (post == null)
            {
                return NotFound();
            }

            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name", post.BlogId);
            ViewData["TagValues"] = string.Join(",", post.Tags.Select(t => t.Text));

            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BlogId,Title,Abstract,Content,ReadyStatus")] Post post, IFormFile? newImage, List<string> tagValues)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // originalPost: a copy of the post from the database before any edits are applied.
                    var originalPost = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == post.Id);

                    originalPost.Updated = DateTime.Now;
                    originalPost.Title = post.Title;
                    originalPost.Abstract = post.Abstract;
                    originalPost.Content = post.Content;
                    originalPost.ReadyStatus = post.ReadyStatus;

                    var newSlug = _slugService.UrlFriendly(post.Title);

                    if(newSlug != originalPost.Slug)
                    {
                        if (_slugService.IsUnique(newSlug))
                        {
                            originalPost.Title = post.Title;
                            originalPost.Slug = newSlug;
                        }
                        else
                        {
                            ModelState.AddModelError("Title", "The Title you provided cannot be used due to it resulting in a duplicate value.");
                            ViewData["TagValues"] = string.Join(",", post.Tags.Select(t => t.Text));
                            return View(post);
                        }
                    }

                    if (newImage is not null)
                    {
                        originalPost.ImageData = await _imageService.EncodeImageAsync(newImage);
                        originalPost.ContentType = _imageService.ContentType(newImage);
                    }

                    // Remove all Tags previously associated with this Post
                    _context.Tags.RemoveRange(originalPost.Tags);
                    
                    // Add in the new Tags from the Edit form
                    foreach(var tagText in tagValues)
                    {
                        _context.Add(new Tag()
                        {
                            PostId = post.Id,
                            BlogUserId = originalPost.BlogUserId,
                            Text = tagText
                        });
                    }
                    
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Description", post.BlogId);
            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", post.BlogUserId);
            return View(post);
        }

        // GET: Posts/Delete/5
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null || _context.Posts == null)
        //    {
        //        return NotFound();
        //    }

        //    var post = await _context.Posts
        //        .Include(p => p.Blog)
        //        .Include(p => p.BlogUser)
        //        .FirstOrDefaultAsync(m => m.Id == id);
        //    if (post == null)
        //    {
        //        return NotFound();
        //    }

        //    return View(post);
        //}

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string slug)
        {
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Blog)
                .Include(p => p.BlogUser)
                .FirstOrDefaultAsync(m => m.Slug == slug);

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var authorId = _userManager.GetUserId(User);

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == id && p.BlogUserId == authorId);

            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
          return (_context.Posts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
