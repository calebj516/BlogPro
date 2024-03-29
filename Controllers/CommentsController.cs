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

namespace TheBlogProject.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<BlogUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Comments
        public async Task<IActionResult> OriginalIndex()
        {
            var blogUserId = _userManager.GetUserId(User);
            var originalComments = await _context.Comments.Where(c => c.BlogUserId == blogUserId).ToListAsync();
            return View("Index", originalComments);
        }

        public async Task<IActionResult> ModeratedIndex()
        {
            var moderatedComments = await _context.Comments.Where(c => c.Moderated != null).ToListAsync();
            return View("Index", moderatedComments);
        }

        //public async Task<IActionResult> DeletedIndex()
        //{
        //Use my soft delete boolean
        //}

        public async Task<IActionResult> Index()
        {
            var authorId = _userManager.GetUserId(User);
            var allComments = await _context.Comments.Where(c => c.PostId == c.Post.Id && c.BlogUserId == authorId).ToListAsync();
            return View(allComments);
        }

        // GET: Comments/Create
        //public IActionResult Create()
        //{
        //    ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id");
        //    ViewData["ModeratorId"] = new SelectList(_context.Users, "Id", "Id");
        //    ViewData["PostId"] = new SelectList(_context.Posts, "Id", "Abstract");
        //    return View();
        //}

        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PostId,Body")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                comment.BlogUserId = _userManager.GetUserId(User);
                comment.Created = DateTime.Now;
                _context.Add(comment);
                await _context.SaveChangesAsync();
                //return RedirectToAction(nameof(Index));
                var originalComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == comment.Id);
                return RedirectToAction("Details", "Posts", new { slug = originalComment.Post.Slug }, "commentSection");
            }

            return View(comment);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }

            var authorId = _userManager.GetUserId(User);
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.BlogUserId == authorId);

            if (comment == null)
            {
                return NotFound();
            }

            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", comment.BlogUserId);
            ViewData["ModeratorId"] = new SelectList(_context.Users, "Id", "Id", comment.ModeratorId);
            ViewData["PostId"] = new SelectList(_context.Posts, "Id", "Abstract", comment.PostId);

            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Body")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var originalComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == comment.Id);
                try
                {
                    originalComment.Body = comment.Body;
                    originalComment.Updated = DateTime.Now;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Posts", new { slug = originalComment.Post.Slug }, "commentSection");
            }
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Moderate(int id, [Bind("Id,Body,ModeratedBody,ModerationType")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var originalComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == comment.Id);
                try
                {
                    originalComment.ModeratedBody = comment.ModeratedBody;
                    originalComment.ModerationType = comment.ModerationType;

                    originalComment.Moderated = DateTime.Now;
                    originalComment.ModeratorId = _userManager.GetUserId(User);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Posts", new { slug = originalComment.Post.Slug }, "commentSection");
            }
            return View(comment);
        }

        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }

            var authorId = _userManager.GetUserId(User);
            var comment = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(m => m.Id == id && m.BlogUserId == authorId);

            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string slug)
        {
            var authorId = _userManager.GetUserId(User);
            var comment = await _context.Comments.FirstOrDefaultAsync(c => c.Id == id && c.BlogUserId == authorId);

            if (comment != null)
            {
                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Posts", new { slug }, "commentSection");
        }

        private bool CommentExists(int id)
        {
            return (_context.Comments?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
