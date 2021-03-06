﻿using MusicStore.ViewModels;
using MusicStoreEntity;
using MusicStoreEntity.UserAndRole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace MusicStore.Controllers
{
    public class StoreController : Controller
    {

        private static readonly EntityDbContext _context = new EntityDbContext();
        /// <summary>
        /// 显示专辑的明细
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET: Store
        public ActionResult Detail(Guid id)
        {
            var detail = _context.Albums.Find(id);
            //显示评论
            var cmt = _context.Replys.Where(x => x.Album.ID == id && x.ParentReply == null)
               .OrderByDescending(x => x.CreateDateTime).ToList();
           
            ViewBag.Cmt = _GetHtml(cmt);
            return View(detail);
        }

        /// <summary>
        /// 点赞
        /// </summary>
        /// <param name="id">回复</param>
        /// <returns></returns>
         [HttpPost]
        public ActionResult Like(Guid id)
        {

            //判断用户是否登录
            if (Session["LoginUserSessionModel"] == null)
                return Json("nologin");

            //判断用户是否对这条回复点赞或踩
            var person = _context.Persons.Find((Session["LoginUserSessionModel"] as LoginUserSessionModel).Person.ID);
            var reply = _context.Replys.Find(id);
            var IsLike = _context.LikeReply.SingleOrDefault(x => x.Reply.ID == reply.ID && x.Person.ID == person.ID);

            //显示评论、排序
            var albumSay = _context.Replys.Where
                (x => x.Album.ID == reply.Album.ID && x.ParentReply == null)
                .OrderByDescending(x => x.CreateDateTime).ToList();

            //刷新当前评论
            var htmlString ="";
            //保存reply实体中like+1或hate+1  LikeReply添加一条记录
            if (IsLike == null || IsLike.Person.ID != person.ID)
            {
                reply.Like += 1;
                var ok = new LikeReply()
                {
                    Reply = reply,
                    IsNotLike = true,
                    Person = person
                };
                _context.LikeReply.Add(ok);
                _context.SaveChanges();

            }

            else
            {
                if (IsLike.IsNotLike == false)
                    return Json("false");
                reply.Like -= 1;
                _context.LikeReply.Remove(IsLike);
                _context.SaveChanges();
            }
            htmlString = _GetHtml(albumSay);
                return Json(htmlString);
            }


        /// <summary>
        /// 踩
        /// </summary>
        /// <param name="id">回复</param>
        /// <returns></returns>
        public ActionResult Hate(Guid id)
        {

            //判断用户是否登录
            if (Session["LoginUserSessionModel"] == null)
                return Json("nologin");

            //判断用户是否对这条回复点赞或踩
            var person = _context.Persons.Find((Session["LoginUserSessionModel"] as LoginUserSessionModel).Person.ID);
            var reply = _context.Replys.Find(id);
            var IsLike = _context.LikeReply.SingleOrDefault(x => x.Person.ID == reply.ID && x.Person.ID == person.ID);
            //保存reply实体中like+1或hate+1  LikeReply添加一条记录
            if (IsLike == null || IsLike.Person.ID != person.ID)
            {
                reply.Hate += 1;
                var ok = new LikeReply()
                {
                    Reply = reply,
                    IsNotLike = false,
                    Person = person
                };
                _context.LikeReply.Add(ok);
                _context.SaveChanges();

                var albumSay = _context.Replys.Where(x => x.Album.ID == reply.Album.ID && x.ParentReply == null).OrderByDescending(x => x.CreateDateTime).ToList();

                var htmlString = _GetHtml(albumSay);

                return Json(htmlString);
            }
            //踩失败或是已经踩过了
            else
            {
                reply.Hate -= 1;
                _context.SaveChanges();
                //显示评论
                var albumSay = _context.Replys.Where(x => x.Album.ID == reply.Album.ID && x.ParentReply == null).OrderByDescending(x => x.CreateDateTime).ToList();

                //刷新
                var htmlString = _GetHtml(albumSay);

                return Json(htmlString);
            }
            
        }



        /// <summary>
        /// 生成回复显示html文本
        /// </summary>
        /// <param name="cmt"></param>
        /// <returns></returns>
        private string _GetHtml(List<Reply> cmt)
        {
            var htmlString = "";

            htmlString += "<ul class='media-list'>";
            foreach (var item in cmt)
            {
                htmlString += "<li class='media'>";
                htmlString += "<div class='media-left'>";
                htmlString += "<img class='media-object' src='" + item.Person.Avarda +
                              "' alt='头像' style='width:40px;border-radius:50%;'>";
                htmlString += "</div>";
                htmlString += "<div class='media-body' id='Content-" + item.ID + "'>";
                htmlString += "<h5 class='media-heading'> <em>" + item.Person.Name + "</em>&nbsp;&nbs  发表于" +
                              item.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + "</h5>";
                htmlString += item.Content;
                htmlString += "</div>";
                //查询当前回复的下一级回复
                var sonCmt = _context.Replys.Where(x => x.ParentReply.ID == item.ID).ToList();
                htmlString += "<h6><a href='#div-editor' class='reply' onclick=\"javascript:GetQuote('" + item.ID +"','"+item.ID+
                               "');\">回复</a>(<a href='#' class='reply'  onclick=\"javascript:ShowCmt('" + item.ID + "');\">" + sonCmt.Count + "</a>)条" +
                               "<a href='#' class='reply' style='margin:0 20px 0 40px'   onclick=\"javascript:Like('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-up'></i>(" + item.Like + ")</a>" +
                               "<a href='#' class='reply' style='margin:0 20px'   onclick=\"javascript:Hate('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-down'></i>(" + item.Hate + ")</a></h6>";

                htmlString += "</li>";

            }
            htmlString += "</ul>";
            return htmlString;
        }



        //评论
        [HttpPost]
        [ValidateInput(false)]//关闭验证
        public ActionResult AddCmt(string id, string cmt, string reply)
        {
            //判断是否登录
            if (Session["LoginUserSessionModel"] == null)
                return Json("nologin");

            var person = _context.Persons.Find((Session["LoginUserSessionModel"] as
              LoginUserSessionModel).Person.ID);
            var album = _context.Albums.Find(Guid.Parse(id));

            //创建回复对象

            var r = new Reply()
            {
                Album = album,
                Person = person,
                Content = cmt,
                Title = ""
            };
            //父级回复
            if (string.IsNullOrEmpty(reply))
            {
                //顶级回复
                r.ParentReply = null;

            }
            else
            {
                r.ParentReply = _context.Replys.Find(Guid.Parse(reply));
            }
            _context.Replys.Add(r);
            _context.SaveChanges();
            //局部刷新显示
            var replies = _context.Replys.Where(x => x.Album.ID == album.ID && x.ParentReply == null)
              .OrderByDescending(x => x.CreateDateTime).ToList();
            return Json(_GetHtml(replies));
        }

        //显示子回复
        [HttpPost]
        public ActionResult showCmts(string pid)
        {
            var htmlString = "";
            //子回复
            Guid id = Guid.Parse(pid);
            var cmts =_context.Replys.Where(x=>x.ParentReply.ID == id).OrderByDescending(x=>x.CreateDateTime).ToList();
            //原回复
            var pcmt = _context.Replys.Find(id);
            htmlString += "<div class=\"modal-header\">";
            htmlString += "<button type=\"button\" class=\"close\" data-dismiss=\"modal\" aria-hidden=\"true\">×</button>";
            htmlString += "<h4 class=\"modal-title\" id=\"myModalLabel\">";
            htmlString += "<em>楼主&nbsp;&nbsp;</em>" + pcmt.Person.Name + "  发表于" + pcmt.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + ":<br/>" + pcmt.Content;
            htmlString += " </h4> </div>";

            htmlString +="<div class=\"modal-body\">";

            //子回复

            htmlString += "<ul class='media-list' style='margin-left:20px;'>";
            foreach(var item in cmts)
            {

                htmlString += "<li class='media'>";
                htmlString += "<div class='media-left'>";
                htmlString += "<img class='media-object' src='" + item.Person.Avarda +
                              "' alt='头像' style='width:40px;border-radius:50%;'>";
                htmlString += "</div>";
                htmlString += "<div class='media-body' id='Content-" + item.ID + "'>";
                htmlString += "<h5 class='media-heading'> <em>" + item.Person.Name + "</em>&nbsp;&nbs  发表于" +
                              item.CreateDateTime.ToString("yyyy年MM月dd日 hh点mm分ss秒") + "</h5>";
                htmlString += item.Content;
                htmlString += "</div>";

                htmlString += "<h6><a href='#div-editor' class='reply' onclick=\"javascript:GetQuote('" + item.ParentReply.ID+ "','" + item.ID +
                              "');\">回复</a>(<a href='#' class='reply'  style='margin:0 20px 0 40px' onclick=\"javascript:Like('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-up'></i>("+item.Like+")</a>"+
                              "<a href='#' class='reply' style='margin:0 20px 0 40px'   onclick=\"javascript:Like('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-up'></i>(" + item.Like + ")</a>" +
                              "<a href='#' class='reply' style='margin:0 20px'  onclick=\"javascript:Hate('" + item.ID + "');\"><i class='glyphicon glyphicon-thumbs-down'></i>(" + item.Hate + ")</a></h6>";
                htmlString += "</li>";

            }

            htmlString += "</ul>";
            htmlString += "</div><div class=\"model-footer\"></div>";

            return Json(htmlString);
        }

        /// <summary>
        /// 按分类显示专辑页
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Browser(Guid id)
        {
            var list = _context.Albums.Where(x => x.Genre.ID == id)
                   .OrderByDescending(x => x.PublisherDate).ToList();
            return View(list);
        }


        /// <summary>
        /// 显示所有的分类
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            var genres = _context.Genres.OrderBy(x=>x.Name).ToList();
            return View(genres);
        }
    }
}