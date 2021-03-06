﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingApp.Data;
using ShoppingApp.Models;
using X.PagedList;

namespace ShoppingApp.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        // 每個分頁最多顯示10筆
        private readonly int pageSize = 10;

        // 注入會用到的工具
        private readonly ApplicationDbContext _context;
        private readonly ILogger _logger;

        public CommentController(ApplicationDbContext context, ILogger<CommentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            // 按照留言的建立日期排序(新->舊)
            return View(await _context.Comment.OrderByDescending(c => c.CreateTime).ToPagedListAsync(page, pageSize));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comment
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        public IActionResult Create(int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Content,ProductId")] Comment comment)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (ModelState.IsValid)
            {
                comment.UserName = User.Identity.Name;
                comment.CreateTime = DateTime.Now;
                _context.Add(comment);
                await _context.SaveChangesAsync();

                // 返回之前的分頁
                int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
                int page = TryGetPage != null ? (int)TryGetPage : 1;
                return RedirectToAction("Index", new { page });
            }
            return View(comment);
        }

        public async Task<IActionResult> Edit(int? id, int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comment.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            return View(comment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Content,UserName,CreateTime,ProductId")] Comment comment)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comment);
                    await _context.SaveChangesAsync();

                    // 返回之前的分頁
                    int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
                    int page = TryGetPage != null ? (int)TryGetPage : 1;
                    return RedirectToAction("Index", new { page });
                }
                catch (DbUpdateConcurrencyException e)
                {
                    _logger.LogError(e.ToString());
                    return RedirectToAction(nameof(Index));
                }
            }
            return View(comment);
        }

        public async Task<IActionResult> Delete(int? id, int returnPage = 0)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return NotFound();

            if (returnPage != 0)
            {
                HttpContext.Session.SetInt32("returnPage", returnPage);
            }

            var comment = await _context.Comment.FindAsync(id);
            _context.Comment.Remove(comment);
            await _context.SaveChangesAsync();
            _logger.LogWarning($"[{User.Identity.Name}]刪除了一筆[{comment.UserName}]的留言");

            // 返回之前的分頁
            int? TryGetPage = HttpContext.Session.GetInt32("returnPage");
            int page = TryGetPage != null ? (int)TryGetPage : 1;
            return RedirectToAction("Index", new { page });
        }

        public async Task<IActionResult> DeleteAll()
        {
            if (User.Identity.Name != AuthorizeManager.SuperAdmin) return NotFound();

            _context.RemoveRange(_context.Comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentExists(int id)
        {
            if (!AuthorizeManager.InAdminGroup(User.Identity.Name)) return false;

            return _context.Comment.Any(e => e.Id == id);
        }
    }
}