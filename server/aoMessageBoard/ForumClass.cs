

using System;
using System.Collections.Generic;
using System.Text;
using Contensive.BaseClasses;


namespace aoForums2
{
    public class forumClass :  Contensive.BaseClasses.AddonBaseClass
    {
        private const string rnFormId = "formId";
        private const string rnForumId = "forumId";
        private const string rnPostId = "postId";
        private const string rnThreadId = "threadId";
        private const string rnNewThreadThreadTitle = "foInputThreadTitle";
        private const string rnNewThreadName = "foInputTitle";
        private const string rnNewThreadTitle = "foInputName";
        private const string rnNewThreadOrg = "foInputOrg";
        private const string rnNewThreadPost = "foTextAreaPost";
        private const string rnCreateKey = "foCreateKey";
        private const string rnIntercept = "foInterceptForm";
        //
        // hidden form input in all forms -- use to flag a form submitted
        //
        private const string rnSourceForm = "forumSourceFormId";
        //
        private const int formIdForumList = 1;
        private const int formIdThreadList = 2;
        private const int formIdPostList = 3;
        private const int formIdNewThread = 4;
        private const int formIdNewPost = 5;
        private const int formIdNewForum = 6;
        private const int formIdProfile = 7;
        //
        private const string profileInstructions = "<p>Use this form to update your online profile.</p>";
        private const string profileNotificationInstructions = "<p>Choose from the list of forums below to be notified of a new post or comment.</p>";
        //
        // ===============================================================================================
        // execute method is the only public
        // ===============================================================================================
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            int sourceFormId = 0;
            int formId = 0;
            int forumId = 0;
            int threadId = 0;
            string interceptForm;
            string s = "";
            string email = "";
            bool isEditing = cp.User.IsEditingAnything;
            string forceInterceptForm = "";
            CPBlockBaseClass block = cp.BlockNew();
            string qs = "";
            string rqs = "";
            string copy = "";
            CPCSBaseClass cs = cp.CSNew();
            //
            cp.Site.SetProperty("forum last display qs", cp.Doc.RefreshQueryString);
            //
            sourceFormId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnSourceForm));
            //
            formId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnFormId));
            forumId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnForumId));
            threadId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnThreadId));
            interceptForm = cp.Doc.GetProperty(rnIntercept, "");
            if ((forumId == 0) & (threadId != 0))
            {
                cs.OpenSQL("select forumId from ccforumThreads where id=" + threadId);
                if (cs.OK())
                {
                    forumId = cs.GetInteger("forumId");
                }
                else
                {
                    threadId = 0;
                }
            }
            //
            //cp.Doc.AddRefreshQueryString(rnFormId, formId.ToString());
            cp.Doc.AddRefreshQueryString(rnForumId, forumId.ToString());
            cp.Doc.AddRefreshQueryString(rnThreadId, threadId.ToString());
            //cp.Doc.AddRefreshQueryString(rnIntercept, interceptForm.ToString());
            rqs = cp.Doc.RefreshQueryString;
            //
            // handle intercept forms
            //
            if (interceptForm != "")
            {
                switch (interceptForm)
                {
                    case "register":
                        if (processRegister(cp))
                        {
                            cp.Response.Redirect("?" + rqs);
                        }
                        break;
                    case "login":
                        if ( cp.User.Login( cp.Doc.GetProperty("username", ""), cp.Doc.GetProperty("password", ""), false) )
                        {
                            cp.Response.Redirect("?" + rqs);
                        }
                        break;
                    case "logout":
                        cp.User.Logout();
                        cp.Response.Redirect("?" + rqs);
                        break;
                    case "password":
                        email = cp.Doc.GetProperty("email", "");
                        if (email == "")
                        {
                            cp.UserError.Add("You must include an email address to have your username and password sent.");
                        }
                        else
                        {
                            cp.Email.sendPassword(email);
                        }
                        break;
                }
            }
            //
            // process current form
            //
            if (sourceFormId != 0)
            {
                switch (sourceFormId)
                {
                    case formIdProfile:
                        formId = processProfile(cp);
                        break;
                    case formIdNewForum:
                        formId = processNewForum(cp);
                        break;
                    case formIdForumList:
                        formId = processForumList(cp);
                        break;
                    case formIdThreadList:
                        formId = processThreadList(cp);
                        break;
                    case formIdNewThread:
                        formId = processNewThread(cp, forumId);
                        break;
                    case formIdNewPost:
                        formId = processNewPost(cp, threadId);
                        break;
                    default:
                        formId = formIdForumList;
                        break;
                }
            }
            //
            // determine next form if not set
            //
            if (formId == 0)
            {
                if ( threadId!=0 ) 
                {
                    formId = formIdPostList;
                }
                else if ( forumId != 0 ) 
                {
                    formId = formIdThreadList;
                }
                else
                {
                    formId = formIdForumList ;
                }
            }
            //
            // Intercept login and register - but leave the formId as-is
            // 
            forceInterceptForm = cp.Doc.GetProperty("forceInterceptForm","");
            if ( forceInterceptForm == "login" ) 
            {
                s = getLogin(cp, formId, forumId, threadId);
            }
            else if (forceInterceptForm == "register")
            {
                s = getRegister(cp, formId, forumId, threadId);
            }
            else if
                (
                    (
                        (forumId != 0) & !userHasAccess(cp, forumId)
                    ) 
                    | 
                    (
                        (!cp.User.IsAuthenticated) 
                        &
                        (
                            (formId == formIdNewThread) 
                            | (formId == formIdNewPost) 
                            | (formId == formIdNewForum)
                        )
                    )
                )
            {
                //
                // accessing a forum that is closed and you have no permissions
                // or accessing a form that requires you to login
                //
                if (cp.Utils.EncodeBoolean(cp.Doc.GetProperty("register", "")))
                {
                    s = getRegister(cp, formId, forumId, threadId);
                }
                else if ( !cp.User.IsAuthenticated )
                {
                    s = getLogin(cp, formId, forumId, threadId);
                }
                else
                {
                    block.OpenLayout("forum - not in group page");
                    s = block.GetHtml();
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnFormId , formIdForumList.ToString(), false);
                    qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", false);
                    s = s.Replace("$forumList$", "?" + qs);
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnIntercept, "logout", true);
                    s = s.Replace("$forumlogin$", "?" + qs);
                }
            }
            else
            {
                if ((!cp.User.IsAdmin) && (formId == formIdNewForum))
                {
                    //
                    // you must be an administrator for these forms
                    //
                    cp.UserError.Add("You must be an administrator to create a new forum.");
                    formId = formIdForumList;
                }
                //
                // verify form requirements
                //
                switch (formId)
                {
                    case formIdPostList:
                        if (threadId == 0) formId = formIdForumList;
                        break;
                    case formIdThreadList:
                        if (forumId == 0) formId = formIdForumList;
                        break;
                    case formIdNewThread:
                        if (forumId == 0) formId = formIdForumList;
                        break;
                    case formIdNewPost:
                        if (threadId == 0) formId = formIdForumList;
                        break;
                    //default:
                    //    s = getForumList(cp, isEditing);
                    //    break;
                }
                //
                // get next form
                //
                switch (formId)
                {
                    case formIdProfile:
                        s = getProfile(cp);
                        break;
                    case formIdNewForum:
                        s = getNewForum(cp);
                        break;
                    case formIdPostList:
                        s = getPostList(cp, threadId, isEditing);
                        break;
                    case formIdThreadList:
                        s = getThreadList(cp, forumId, isEditing);
                        break;
                    case formIdNewThread:
                        s = getNewThread(cp, forumId);
                        break;
                    case formIdNewPost:
                        s = getNewPost(cp, threadId);
                        break;
                    default:
                        s = getForumList(cp, isEditing);
                        break;


                }
            }
            //
            // update login, logout, register buttons
            //
            rqs = cp.Doc.RefreshQueryString;
            block.Load(s);
            if (!cp.User.IsAuthenticated)
            {
                //
                // not authenticated - block anything marked block
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, "forceInterceptForm", "login", true);
                copy = "<a class=\"loginLightbox\" href=\"?" + qs + "\">Login</a>";
                block.SetOuter(".foButtonLogin", copy);
                qs = cp.Utils.ModifyQueryString(qs, "forceInterceptForm", "register", true);
                copy = "<a href=\"?" + qs + "\">Register</a>";
                block.SetOuter(".foButtonRegister", copy);
                block.SetOuter(".foButtonLogout", "");
                block.SetOuter(".foButtonProfile", "");
            }
            else
            {
                //
                // authenticated
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnIntercept, "logout", true);
                copy = "<a href=\"?" + qs + "\">Logout</a>";
                block.SetOuter(".foButtonLogout", copy);
                block.SetOuter(".foButtonLogin", "");
                block.SetOuter(".foButtonRegister", "");
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdProfile.ToString(), true);
                copy = "<a href=\"?" + qs + "\">Update/Edit Profile</a>";
                block.SetOuter(".foButtonProfile", copy);
            }
            s = block.GetHtml();
            //
            // return result
            //
            return s;
        }
        //
        // ===============================================================================================
        // process new thread form
        // ===============================================================================================
        //
        private int processNewThread(CPBaseClass cp, int forumId )
        {
            CPCSBaseClass cs = cp.CSNew();
            int nextFormId = formIdNewThread;
            string sql = "";
            int threadId = 0;
            int createKey = 0;
            bool blockForm = false;
            //
            if (cp.Doc.GetProperty(rnNewThreadThreadTitle, "") == "")
            {
                cp.UserError.Add("Your thread must include a subject.");
            }
            else if (cp.Doc.GetProperty(rnNewThreadPost, "") == "")
            {
                cp.UserError.Add("Your thread must have an initial post.");
            }
            else if (!cp.User.IsAuthenticated)
            {
                cp.UserError.Add("You must be logged in to create a thread.");
                nextFormId = formIdThreadList;
            }
            if (cp.UserError.OK())
            {
                //
                // test for re-submit
                //
                createKey = cp.Utils.EncodeInteger(cp.Doc.GetProperty(rnCreateKey, ""));
                if ( createKey != 0 )
                {
                    blockForm = cs.Open("forum threads", "createKey=" + createKey.ToString(),"",true,"",1,1);
                    cs.Close();

                }
                if (!blockForm)
                {
                    //
                    // save thread
                    //
                    cs.Insert("forum threads");
                    threadId = cs.GetInteger("id");
                    cs.SetField("createKey", createKey.ToString());
                    cs.SetFormInput("name", "foInputThreadTitle");
                    cs.SetFormInput("copy", "foTextAreaPost");
                    cs.SetField( "forumId", forumId.ToString());
                    cs.Close();
                    //
                    // save uploaded file
                    //
                    if (cp.Doc.GetProperty("foInputFile","") != "")
                    {
                        cs.Insert("forum files");
                        cs.SetFormInput("name", "foInputFile");
                        cs.SetFormInput("filename", "foInputFile");
                        cs.SetField( "forumThreadId", threadId.ToString());
                        cs.Close();
                    }
                    //
                    // housekeep the changes
                    //
                    cp.Doc.SetProperty("recordId", threadId.ToString());
                    threadHousekeepClass obj = new threadHousekeepClass();
                    obj.Execute( cp );
                }
                nextFormId = formIdThreadList;
            }
            sql = "select count(id) as cnt from ccforumThreads where forumid=" + forumId;
            if (cs.OpenSQL(sql))
            {
                cp.Db.ExecuteSQL("update ccforums set threads=" + cs.GetInteger("cnt").ToString() + " where id=" + forumId, "", "1", "1", "1");
            }
            cs.Close();
            //
            return nextFormId;
        }
        //
        // ===============================================================================================
        // process Forum List
        // ===============================================================================================
        //
        private int processForumList(CPBaseClass cp)
        {
            return formIdForumList;
        }
        //
        // ===============================================================================================
        // process Thread List
        // ===============================================================================================
        //
        private int processThreadList(CPBaseClass cp)
        {
            return formIdThreadList;
        }
        //
        // ===============================================================================================
        // process Post List
        // ===============================================================================================
        //
        private int processPostList(CPBaseClass cp)
        {
            return formIdPostList;
        }
        //
        // ===============================================================================================
        // get Forum List
        // ===============================================================================================
        //
        private string getForumList(CPBaseClass cp, bool isEditing)
        {
            const string copyRecordName = "forums - Forum Intro";
            //
            CPBlockBaseClass block = cp.BlockNew();
            CPBlockBaseClass blockPublic = cp.BlockNew();
            CPBlockBaseClass blockPrivate = cp.BlockNew();
            CPBlockBaseClass listItemOdd = cp.BlockNew();
            CPBlockBaseClass listItemEven = cp.BlockNew();
            CPBlockBaseClass listItem;
            CPCSBaseClass cs = cp.CSNew();
            string imageUrl = "";
            string listPublic = "";
            string listPrivate = "";
            int ptr = 0;
            string copy = "";
            string sql = "";
            string forumATag = "";
            //string postATag = "";
            string qs = "";
            string rqs = "";
            DateTime lastDate;
            string forumName;
            string forumDescription;
            int forumId;
            bool segregatePrivate = cp.Utils.EncodeBoolean(cp.Site.GetProperty("Forums - Segregate Private Forums","0"));
            string forumLayout = "";
            int listPrivateCnt = 0;
            int listPublicCnt = 0;
            bool isAuthenticated = cp.User.IsAuthenticated;
            bool forumBlocked = true;
            //
            block.OpenLayout("forums - forum list view");
            listItemOdd.Load(block.GetOuter(".foItemOdd"));
            listItemEven.Load(block.GetOuter(".foItemEven"));
            listPublic = block.GetOuter(".foHead");
            listPrivate = listPublic;
            //
            rqs = cp.Doc.RefreshQueryString;
            rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
            //
            copy = cp.Content.GetCopy(copyRecordName, "<p>Welcome to our forums.</p>");
            //if (isEditing)
            //{
            //    copyRecordId = cp.Content.GetRecordID("copy content", copyRecordName);
            //    copy = cp.Content.GetEditLink("copy content", copyRecordId.ToString(), false, copyRecordName, true) + copy;
            //}
            block.SetInner(".foForumIntro", copy);
            //
            if (!cp.User.IsAuthenticated)
            {
                //
                // not authenticated - block anything marked block
                //
                //qs = rqs;
                //qs = cp.Utils.ModifyQueryString(qs, "forceInterceptForm", "login", true);
                //copy = "<a href=\"?" + qs + "\">Login</a>";
                //block.SetOuter(".foButtonLogin", copy);
                //qs = cp.Utils.ModifyQueryString(qs, "forceInterceptForm", "register", true);
                //copy = "<a href=\"?" + qs + "\">Register</a>";
                //block.SetOuter(".foButtonRegister", copy);
                block.SetOuter(".foButtonNewForum", "");
                //block.SetOuter(".foButtonLogout", "");
                //
                //sql = ""
                //    + "select f.id"
                //    + ",f.block"
                //    + ",f.name"
                //    + ",f.overview"
                //    + ",f.threads"
                //    + ",f.posts"
                //    + ",p.name as lastTitle"
                //    + ",p.dateAdded as lastDate"
                //    + ",m.name as authorName"
                //    + ",m.nickname as authorNickname"
                //    + ",f.imageFilename"
                //    + ",f.lastpostid "
                //    + ",f.contentControlId "
                //    + " from ((ccforums f"
                //    + " left join ccforumPosts p on p.id=f.lastpostid)"
                //    + " left join ccmembers m on m.id=p.createdby)"
                //    + " where (f.active<>0)"
                //    + " and ((f.block is null)or(f.block=0))"
                //    + " order by p.dateAdded desc"
                //    + "";
            }
            else if (cp.User.IsAdmin)
            {
                //
                // admin - allow new forum button and show all forums
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdNewForum.ToString(), true);
                copy = "<a href=\"?" + qs + "\">New Forum</a>";
                block.SetOuter(".foButtonNewForum", copy);
                //qs = rqs;
                //qs = cp.Utils.ModifyQueryString(qs, rnIntercept, "logout", true);
                //copy = "<a href=\"?" + qs + "\">Logout</a>";
                //block.SetOuter(".foButtonLogout", copy);
                //block.SetOuter(".foButtonLogin", "");
                //block.SetOuter(".foButtonRegister", "");
                //
                // block nothing
                //
                //sql = ""
                //    + "select f.id"
                //    + ",f.block"
                //    + ",f.name"
                //    + ",f.overview"
                //    + ",f.threads"
                //    + ",f.posts"
                //    + ",p.name as lastTitle"
                //    + ",p.dateAdded as lastDate"
                //    + ",m.name as authorName"
                //    + ",m.nickname as authorNickname"
                //    + ",f.imageFilename"
                //    + ",f.lastpostid "
                //    + ",f.contentControlId"
                //    + " from ((ccforums f"
                //    + " left join ccforumPosts p on p.id=f.lastpostid)"
                //    + " left join ccmembers m on m.id=p.createdby)"
                //    + " where (f.active<>0)"
                //    + " order by p.dateAdded desc"
                //    + "";
            }
            else
            {
                //
                // authenticated but not admin - blocking
                //
                block.SetOuter(".foButtonNewForum", "");
                //qs = rqs;
                //qs = cp.Utils.ModifyQueryString(qs, rnIntercept, "logout", true);
                //copy = "<a href=\"?" + qs + "\">Logout</a>";
                //block.SetOuter(".foButtonLogout", copy);
                //block.SetOuter(".foButtonLogin", "");
                //block.SetOuter(".foButtonRegister", "");
                //
                //sql = ""
                //    + "select f.id"
                //    + ",f.block"
                //    + ",f.name"
                //    + ",f.overview"
                //    + ",f.threads"
                //    + ",f.posts"
                //    + ",p.name as lastTitle"
                //    + ",p.dateAdded as lastDate"
                //    + ",m.name as authorName"
                //    + ",m.nickname as authorNickname"
                //    + ",f.imageFilename"
                //    + ",f.lastpostid "
                //    + ",f.contentControlId "
                //    + " from ((((ccforums f"
                //    + " left join ccforumPosts p on p.id=f.lastpostid)"
                //    + " left join ccmembers m on m.id=p.createdby)"
                //    + " left join ccforumGroupRules gr on gr.forumId=f.id)"
                //    + " left join ccMemberRules mr on mr.groupId=gr.groupId)"
                //    + " where (f.active<>0)"
                //    + " and ((mr.memberid=" + cp.User.Id + ")or(f.block is null)or(f.block=0))"
                //    + " group by f.id,f.block,f.name,f.overview,f.threads,f.posts,p.name ,p.dateAdded,m.name,m.nickname,f.imageFilename,f.lastpostid,f.contentControlId"
                //    + " order by p.dateAdded desc"
                //    + "";
            }
            //
            // new direction -- list all forums, but if blocked put login button over threads/posts
            //
            sql = ""
                + "select f.id"
                + ",f.block"
                + ",f.name"
                + ",f.overview"
                + ",f.threads"
                + ",f.posts"
                + ",p.name as lastTitle"
                + ",p.dateAdded as lastDate"
                + ",m.name as authorName"
                + ",m.nickname as authorNickname"
                + ",f.imageFilename"
                + ",f.lastpostid "
                + ",f.contentControlId"
                + " from ((ccforums f"
                + " left join ccforumPosts p on p.id=f.lastpostid)"
                + " left join ccmembers m on m.id=p.createdby)"
                + " where (f.active<>0)"
                + " order by p.dateAdded desc"
                + "";
            cs.OpenSQL2(sql, "", 100, 1);
            ptr = 0;
            while ( cs.OK() ) {
                if ((ptr % 2) == 0)
                {
                    listItem = listItemEven;
                }
                else
                {
                    listItem = listItemOdd;
                }
                forumId = cs.GetInteger( "id" );
                //
                forumBlocked = (!isAuthenticated & cs.GetBoolean("block"));
                forumATag = "<a";
                if (forumBlocked)
                {
                    forumATag += " class=\"loginLightbox\"";
                }
                qs = cp.Utils.ModifyQueryString(rqs, rnForumId, cs.GetText("id"), true);
                forumATag += " href=\"?" + qs + "\">";
                qs = cp.Utils.ModifyQueryString(rqs, rnPostId, cs.GetText("lastpostid"), true);
                //postATag = "<a href=\"?" + qs + "\">";
                imageUrl = cs.GetText("imageFilename");
                if ( imageUrl == "" ) 
                {
                    imageUrl = "/aoForums/defaultForumIcon.png";
                } else {
                    imageUrl = cp.Site.FilePath + imageUrl;
                }
                listItem.SetInner(".foOverviewImage", forumATag + "<img src=\"" + imageUrl + "\" width=\"80\" height=\"80\"></a>");
                copy = "";
                //
                forumName = cs.GetText("name");
                if ( forumName=="")
                {
                    forumName = "Forum " + forumId.ToString();
                }
                forumName = forumATag + forumName + "</a>";
                if (isEditing)
                {
                    forumName = cs.GetEditLink(false) + forumName;
                }
                listItem.SetInner(".foOverviewName", forumName);
                //
                forumDescription = cs.GetText("overview");
                if ( forumDescription!="")
                {
                    forumDescription = forumATag + forumDescription + "</a>";
                }
                listItem.SetInner(".foOverviewCopy", forumDescription);
                listItem.SetInner(".foThreads", cp.Utils.EncodeInteger( cs.GetText("threads")).ToString());
                listItem.SetInner(".foPosts", cp.Utils.EncodeInteger(  cs.GetText("posts")).ToString());
                lastDate = cs.GetDate("lastDate");
                if (lastDate < new DateTime(2000, 1, 1))
                {
                    listItem.SetInner(".foLastPost", "");
                    //listItem.SetInner(".foLastTitle", "");
                    //listItem.SetInner(".foAuthorName", "");
                    //listItem.SetInner(".foLastDate", "");
                }
                else
                {

                    //listItem.SetInner(".foLastTitle", postATag + cs.GetText("lastTitle") + "</a>");
                    copy = cs.GetText("authorNickname").Trim();
                    if (copy=="")
                    {
                        copy = cs.GetText("authorName");
                    }
                    listItem.SetInner(".foAuthorName", copy);
                    listItem.SetInner(".foLastDate", lastDate.ToShortDateString());
                }
                if (segregatePrivate & cs.GetBoolean("block"))
                {
                    listPrivate += listItem.GetHtml();
                    listPrivateCnt += 1;
                }
                else
                {
                    listPublic += listItem.GetHtml();
                }
                listPublicCnt += 1;
                listPublicCnt += 1;
                ptr += 1;
                cs.GoNext();
            }
            if (segregatePrivate & (listPrivateCnt > 0))
            {
                forumLayout = block.GetOuter(".foForums");
                //
                blockPrivate.Load(forumLayout);
                blockPrivate.SetInner(".foForums", listPrivate );
                blockPrivate.SetInner(".foOverview", "Closed Forums");
                //
                blockPublic.Load(forumLayout);
                blockPublic.SetInner(".foForums", listPublic);
                blockPublic.SetInner(".foOverview", "Public Forums");
                //
                block.SetOuter(".foForums", blockPublic.GetHtml() + blockPrivate.GetHtml());
            }
            else
            {
                block.SetInner(".foForums", listPublic);
            }
            return block.GetHtml();
        }
        //
        // ===============================================================================================
        // get Forum List
        // ===============================================================================================
        //
        private string getThreadList(CPBaseClass cp, int forumId, bool isEditing)
        {
            string s = "";
            try
            {
                CPBlockBaseClass block = cp.BlockNew();
                CPBlockBaseClass listItemOdd = cp.BlockNew();
                CPBlockBaseClass listItemEven = cp.BlockNew();
                CPBlockBaseClass listItem;
                CPCSBaseClass cs = cp.CSNew();
                CPCSBaseClass csThread = cp.CSNew();
                string imageUrl = "";
                string list = "";
                int ptr = 0;
                string copy = "";
                string sql = "";
                string threadATag = "";
                //string postATag = "";
                string qs = "";
                string rqs = "";
                int threadId;
                DateTime lastDate;
                string startedByName = "";
                //string breadCrumb = "";
                string threadCopy = "";
                string threadName = "";
                string threadEditLink = "";
                string threadDateAddedText = "";
                string lastPostName = "";
                string forumName = "";
                string forumOverview = "";
                string forumCopy = "";
                //
                if (!userHasAccess(cp, forumId))
                {
                    s = getLogin( cp, formIdThreadList, forumId, 0);
                    //if ( cp.Utils.EncodeBoolean( cp.Doc.GetProperty( "register", "" )))
                    //{
                    //   s = getRegister( cp, formIdThreadList, forumId, 0);
                    //}
                    //else
                    //{
                    //   s = getLogin( cp, formIdThreadList, forumId, 0);
                    //}
                }
                else
                {
                    if (cs.Open("forums", "id=" + forumId.ToString(),"",true,"",1,1))
                    {
                        forumName = cs.GetText("name");
                        forumOverview = cs.GetText("overview");
                        forumCopy = cs.GetText("copy");
                    }
                    cs.Close();

                    block.OpenLayout("forums - thread list view");
                    listItemOdd.Load(block.GetOuter(".foItemOdd"));
                    listItemEven.Load(block.GetOuter(".foItemEven"));
                    list = block.GetOuter(".foHead");
                    rqs = cp.Doc.RefreshQueryString;
                    rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
                    rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
                    rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
                    rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
                    //
                    sql = ""
                        + "select t.id,t.viewCnt,t.replyCnt"
                        + " ,f.id as forumId"
                        + " ,p.id as postId,p.dateAdded as dateLastReply"
                        + " ,mt.name as startedByName"
                        + " ,mt.nickname as startedByNickname"
                        + " ,mp.name as lastPostName"
                        + " ,mp.nickname as lastPostNickname"
                        + "  from ((((ccforums f"
                        + "  left join ccforumThreads t on t.forumid=f.id)"
                        + "  left join ccforumPosts p on p.id=t.lastpostid)"
                        + "  left join ccmembers mt on mt.id=t.createdby)"
                        + "  left join ccmembers mp on mp.id=p.createdby)"
                        + "  where ((t.active<>0)or(t.active is null))and(f.Id=" + forumId.ToString() + ")"
                        + "  group by "
                        + " t.id,t.viewCnt,t.replyCnt"
                        + " ,p.id,p.dateAdded"
                        + " ,mt.name,mt.nickname"
                        + " ,mp.name,mp.nickname"
                        + " ,f.id"
                        + "  order by t.id desc";
                    //+ "select t.id,t.name,t.copy as threadCopy,t.createdBy,t.dateAdded as dateThreadAdded,t.viewCnt,t.replyCnt,t.imageFilename,t.lastpostid,t.contentControlId"
                    cs.OpenSQL2(sql, "", 10, 1);
                    ptr = 0;
                    if (!cs.OK())
                    {
                        list = "";
                        qs = rqs;
                        qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                        qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                        block.Clear();
                        block.Append("<p>The Forum you requested could not be found. Please return to the <a href=\"?" + qs + "\">Forum list</a>.</p>");
                    }
                    else
                    {
                        //
                        // title over thread list
                        //
                        qs = rqs;
                        qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                        qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                        copy = forumName;
                        block.SetInner(".foForumTitle", copy);
                        copy = forumCopy;
                        if (copy.Length < 20)
                        {
                            copy = forumOverview;
                        }
                        block.SetInner(".foForumCopy", copy);
                        block.SetInner(".foBreadCrumb", "<a href=\"?" + qs + "\">Forums</a>&nbsp;»&nbsp;" + encodeHtml( cp, forumName));
                        //
                        copy = "<a";
                        if (!cp.User.IsAuthenticated)
                        {
                            copy += " class=\"loginLightbox\"";
                        }
                        qs = rqs;
                        qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdNewThread.ToString(), true);
                        qs = cp.Utils.ModifyQueryString(qs, rnForumId, forumId.ToString(), true);
                        copy += " href=\"?" + qs + "\">Start Thread</a>";
                        block.SetOuter(".foAddThreadButton", copy);
                        //
                        // thread list
                        //
                        while (cs.OK())
                        {
                            if ((ptr % 2) == 0)
                            {
                                listItem = listItemEven;
                            }
                            else
                            {
                                listItem = listItemOdd;
                            }
                            //
                            threadId = cs.GetInteger("id");
                            //
                            if (threadId == 0)
                            {
                                //
                                // no threads found
                                //
                                listItem.SetInner(".foOverviewImage", "<img src=\"/aoForums/defaultThreadIcon.png\" width=\"80\" height=\"80\">");
                                listItem.SetInner(".foOverviewCopy", "This forum has no threads");
                                listItem.SetInner(".foReplies", "0");
                                listItem.SetInner(".foViews", "0");
                                listItem.SetInner(".foLastAuthor", "");
                                listItem.SetInner(".foLastDate", "");
                            }
                            else
                            {
                                //
                                // list threads
                                //
                                if (csThread.Open("forum threads", "id=" + threadId, "", true, "", 1, 1))
                                {
                                    imageUrl = csThread.GetText("imageFilename");
                                    threadName = csThread.GetText("name");
                                    if (threadName == "")
                                    {
                                        threadName = "Thread " + threadId.ToString();
                                    }
                                    threadCopy = csThread.GetText("copy");
                                    threadEditLink = "";
                                    if (isEditing)
                                    {
                                        threadEditLink = csThread.GetEditLink(false);
                                    }
                                    threadDateAddedText = csThread.GetDate("dateAdded").ToShortDateString();
                                }
                                csThread.Close();
                                qs = cp.Utils.ModifyQueryString(rqs, rnThreadId, threadId.ToString(), true);
                                threadATag = "<a href=\"?" + qs + "\">";
                                if (imageUrl == "")
                                {
                                    imageUrl = "/aoForums/defaultThreadIcon.png";
                                }
                                else
                                {
                                    imageUrl = cp.Site.FilePath + imageUrl;
                                }
                                listItem.SetInner(".foOverviewImage", threadATag + "<img src=\"" + imageUrl + "\" width=\"80\" height=\"80\"></a>");
                                copy = "<b>" + threadName + "</b>";
                                startedByName = cs.GetText("startedByNickname");
                                if (startedByName == "")
                                {
                                    startedByName = cs.GetText("startedByName");
                                }
                                if (startedByName != "")
                                {
                                    copy += " - Started By " + startedByName;
                                }
                                copy = "<div>" + copy + "</div>";
                                if (threadCopy != "")
                                {
                                    copy += "<p>" + threadCopy + "</p>";
                                }
                                copy = threadEditLink + threadATag + copy + "</a>";
                                listItem.SetInner(".foOverviewCopy", copy);
                                //
                                listItem.SetInner(".foReplies", cp.Utils.EncodeText(cs.GetInteger("replyCnt")));
                                listItem.SetInner(".foViews", cp.Utils.EncodeText(cs.GetInteger("viewCnt")));
                                //
                                lastDate = cs.GetDate("dateLastReply");
                                if (cs.GetInteger("postId") == 0)
                                {
                                    //
                                    // no replies, list inital thread post
                                    //
                                    listItem.SetInner(".foAuthorName", startedByName);
                                    listItem.SetInner(".foLastDate", threadDateAddedText);
                                }
                                else
                                {
                                    lastPostName = cs.GetText("lastPostNickname");
                                    if (lastPostName == "")
                                    {
                                        lastPostName = cs.GetText("lastPostName");
                                    }
                                    listItem.SetInner(".foAuthorName", lastPostName);
                                    listItem.SetInner(".foLastDate", lastDate.ToShortDateString());
                                }
                            }
                            list += listItem.GetHtml();
                            ptr += 1;
                            cs.GoNext();
                        }
                    }
                    cs.Close();
                    if (list != "")
                    {
                        block.SetInner(".foThreads", list);
                    }
                    s = block.GetHtml();
                }
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e ,"getThreadList Trap");
            }
           return s;
        }
        //
        // ===============================================================================================
        // get Post List
        // ===============================================================================================
        //
        private string getPostList(CPBaseClass cp, int threadId, bool isEditing)
        {
            string s = "";
            try
            {
                CPBlockBaseClass block = cp.BlockNew();
                CPBlockBaseClass listItemOdd = cp.BlockNew();
                CPBlockBaseClass listItemEven = cp.BlockNew();
                CPBlockBaseClass listItemPost = cp.BlockNew();
                CPBlockBaseClass listItem = cp.BlockNew();
                CPBlockBaseClass fileListBlock = cp.BlockNew();
                //CPBlockBaseClass listItem;
                CPCSBaseClass cs = cp.CSNew();
                //string imageUrl = "";
                string list = "";
                int ptr = 0;
                string copy = "";
                string sql = "";
                //string threadATag = "";
                //string postATag = "";
                string qs = "";
                string rqs = "";
                string forumHyperLink;
                //DateTime lastDate;
                //string startedByName = "";
                //string breadCrumb = "";
                DateTime postDate;
                string postfileName = "";
                string postFileTitle = "";
                int pos = 0;
                string fileList = "";
                int postId = 0;
                string threadName;
                string membername = "";
                string posterName = "";
                //
                block.OpenLayout("forums - post list view");
                listItemPost.Load(block.GetOuter(".foPostRow"));
                listItemOdd.Load(block.GetOuter(".foItemOdd"));
                listItemEven.Load(block.GetOuter(".foItemEven"));
                rqs = cp.Doc.RefreshQueryString;
                rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
                rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
                rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
                rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
                //
                // ------------------------
                //  post details
                // ------------------------
                //
                sql = "select"
                    + " t.id"
                    + " ,t.name"
                    + " ,t.contentControlId"
                    + " ,t.copy as threadCopy"
                    + " ,t.dateAdded as dateAdded"
                    + " ,t.replyCnt as replyCnt"
                    + " ,m.id as memberId"
                    + " ,m.title as memberTitle"
                    + " ,m.nickname as MemberNickname"
                    + " ,m.name as MemberName"
                    + " ,m.company as MemberCompany"
                    + " ,o.name as OrgName"
                    + " ,f.id as forumId"
                    + " ,f.name as forumName"
                    + " ,ff.name as postFileTitle"
                    + " ,ff.filename as postFileName"
                    + " from ((((ccForumThreads t"
                    + " left join ccMembers m on m.id=t.createdBy)"
                    + " left join ccForums f on f.id=t.forumId)"
                    + " left join organizations o on o.id=m.organizationid)"
                    + " left join ccForumFiles ff on ff.forumThreadId=t.id)"
                    + " where t.id=" + threadId
                    + "";
                cs.OpenSQL2(sql, "", 10, 1);
                //ptr = 0;
                if (!cs.OK())
                {
                    list = "";
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                    qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                    qs = cp.Utils.ModifyQueryString(qs, rnThreadId, "", true);
                    block.Clear();
                    block.Append("<p>The Thread you requested could not be found. Please return to the <a href=\"?" + qs + "\">Forum list</a>.</p>");
                }
                else
                {
                    //
                    // title over  list
                    //
                    threadName = cs.GetText("name");
                    block.SetInner(".foThreadTitle", threadName);
                    copy = "";
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                    qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                    copy += "<a href=\"?" + qs + "\">Forums</a>";
                    //
                    qs = cp.Utils.ModifyQueryString(qs, rnForumId, cs.GetText("forumId"), true);
                    forumHyperLink = "<a href=\"?" + qs + "\">" + encodeHtml(cp,cs.GetText("forumName")) + "</a>";
                    copy += "&nbsp;»&nbsp;" + forumHyperLink;
                    block.SetInner(".foForumTitle", forumHyperLink);
                    //
                    copy += "&nbsp;»&nbsp;" + encodeHtml(cp, threadName) + "";
                    block.SetInner(".foBreadCrumb", copy);
                    //
                    // post row
                    //
                    membername = cs.GetText("memberNickname");
                    if (membername == "")
                    {
                        membername = cs.GetText("memberName");
                    }
                    block.SetInner(".foPosterName", membername);
                    block.SetInner(".foPosterTitle", cs.GetText("memberTitle"));
                    copy = cs.GetText("orgName");
                    if (copy == "")
                    {
                        copy = cs.GetText("memberCompany");
                    }
                    block.SetInner(".foPosterCompany", copy);
                    block.SetInner(".foPosterDate", cs.GetDate("dateAdded").ToShortDateString());
                    //
                    copy = threadName;
                    if (isEditing)
                    {
                        copy = cs.GetEditLink(false) + copy;
                    }
                    block.SetInner(".foPostTitle", copy);
                    block.SetInner(".foPostBody", cs.GetText("threadCopy"));
                    //
                    copy = "<a";
                    if (!cp.User.IsAuthenticated)
                    {
                        copy += " class=\"loginLightbox\"";
                    }
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdNewPost.ToString(), true);
                    qs = cp.Utils.ModifyQueryString(qs, rnThreadId, threadId.ToString(), true);
                    copy += " href=\"?" + qs + "\">Add Reply</a>";
                    block.SetInner(".foPostReplyButton", copy);
                    //
                    block.SetInner(".foReplyCnt", cs.GetInteger("replyCnt").ToString());
                    //
                    // list all the uploaded files for this thread
                    //
                    postfileName = cs.GetText("postFileName");
                    if (postfileName == "")
                    {
                        block.SetOuter( ".foPosterFileList", "" );
                    }
                    else
                    {
                        fileListBlock.Load(block.GetInner(".foPosterFileList"));
                        fileList = "";
                        while (postfileName != "") 
                        {
                            postFileTitle = cs.GetText("postFileTitle");
                            if (postFileTitle == "")
                            {
                                postFileTitle = postfileName;
                                pos = postFileTitle.LastIndexOf("/");
                                if (pos != 0)
                                {
                                    postFileTitle = postFileTitle.Substring(pos + 1);
                                }
                            }
                            copy = "<a target=\"_blank\" href=\"" + cp.Site.FilePath + postfileName + "\">" + encodeHtml(cp,postFileTitle) + "</a>";
                            fileListBlock.SetInner(".foPosterFile", copy);
                            fileList += fileListBlock.GetHtml();
                            postfileName = "";
                            cs.GoNext();
                            if (cs.OK())
                            {
                                postfileName = cs.GetText("postFileName");
                            }
                        }
                        block.SetInner(".foPosterFileList", fileList);
                    }
                    //
                }
                cs.Close();
                //
                // Build list as replacement for UL items
                //
                list = "";
                list += block.GetOuter(".foHead");
                list += block.GetOuter(".foPostRow");
                list += block.GetOuter(".foRepliesRow");
                //
                // ------------------------
                //  list of replies
                // ------------------------
                //
                sql = "select"
                    + " p.id"
                    + ",p.contentControlId"
                    + ",p.name"
                    + ",p.copy as postBody"
                    + ",p.id as postId"
                    + ",p.dateAdded as postDate"
                    + ",m.id as posterId"
                    + ",m.title as posterTitle"
                    + ",m.name as posterName"
                    + ",m.nickname as posterNickname"
                    + ",m.company as posterCompany"
                    + ",o.name as posterOrgName"
                    + ",f.name as postFileTitle"
                    + ",f.filename as postFilename"
                    + " from (((ccforumposts p"
                    + " left join ccmembers m on m.id=p.createdBy)"
                    + " left join organizations o on o.id=m.organizationid)"
                    + " left join ccForumFiles f on f.forumPostId=p.id)"
                    + " where p.threadid=" + threadId
                    + "";
                cs.OpenSQL2(sql, "", 10, 1);
                while (cs.OK())
                {
                    postId = cs.GetInteger("postId");
                    //
                    if ((ptr % 2) == 0)
                    {
                        listItem = listItemEven;
                    }
                    else
                    {
                        listItem = listItemOdd;
                    }
                    posterName = cs.GetText("posterNickname");
                    if (posterName == "")
                    {
                        posterName = cs.GetText("posterName");
                    }
                    listItem.SetInner(".foPosterName", posterName);
                    listItem.SetInner(".foPosterTitle", cs.GetText("posterTitle"));
                    copy = cs.GetText("posterOrgName");
                    if (copy == "")
                    {
                        copy = cs.GetText("posterCompany");
                    }
                    listItem.SetInner(".foPosterCompany", copy);
                    //
                    postDate = cs.GetDate("postDate");
                    if (postDate == DateTime.MinValue)
                    {
                        listItem.SetInner(".foPostDate", "");
                    }
                    else
                    {
                        listItem.SetInner(".foPostDate", postDate.ToShortDateString());
                    }
                    //listItem.SetInner(".foPostTitle", cs.GetText("postTitle"));
                    postfileName = cs.GetText("postFilename");
                    //
                    copy = cp.Utils.ConvertText2HTML(  cs.GetText("postBody"));
                    if (isEditing)
                    {
                        copy = cs.GetEditLink(false) + copy;
                    }

                    listItem.SetInner(".foPostBody", copy);
                    //--------------------------------------------------
                    //
                    // list all the uploaded files for this thread
                    //
                    postfileName = cs.GetText("postFileName");
                    postFileTitle = cs.GetText("postFileTitle");
                    if (postfileName == "")
                    {
                        cs.GoNext();
                        listItem.SetOuter(".foPostFileList", "");
                    }
                    else
                    {
                        fileListBlock.Load(block.GetInner(".foPostFileList"));
                        fileList = "";
                        while (postfileName != "")
                        {
                            if (postFileTitle == "")
                            {
                                postFileTitle = postfileName;
                                pos = postFileTitle.LastIndexOf("/");
                                if (pos != 0)
                                {
                                    postFileTitle = postFileTitle.Substring(pos + 1);
                                }
                            }
                            copy = "<a target=\"_blank\" href=\"" + cp.Site.FilePath + postfileName + "\">" + encodeHtml(cp,postFileTitle) + "</a>";
                            fileListBlock.SetInner(".foPostFile", copy);
                            fileList += fileListBlock.GetHtml();
                            postfileName = "";
                            cs.GoNext();
                            if (cs.OK())
                            {
                                if ( postId == cs.GetInteger( "postId" ))
                                {
                                    postfileName = cs.GetText("postFileName");
                                    postFileTitle = cs.GetText("postFileTitle");
                                }
                            }
                        }
                        listItem.SetInner(".foPostFileList", fileList);
                    }
                    //--------------------------------------------------
                    list += listItem.GetHtml();
                    ptr += 1;
                }
                cs.Close();
                // ------------------------
                if (list != "")
                {
                    block.SetInner(".foPosts", list);
                }
                s = block.GetHtml();
                //
                // update views
                //
                sql = "update ccforumThreads set viewCnt=viewCnt+1 where id=" + threadId;
                cp.Db.ExecuteSQL( sql ,"", "1", "1","1");
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "getPostList Trap");
            }
            return s;
        }
        //
        // ===============================================================================================
        // get New Thread
        // ===============================================================================================
        //
        private string getNewThread(CPBaseClass cp, int forumId )
        {
            string s = "";
            string forumName = "";
            string forumCopy = "";
            string qs;
            string rqs = cp.Doc.RefreshQueryString;
            string zs = "";
            CPCSBaseClass cs = cp.CSNew();
            string company = "";
            Random random = new Random();
            //
            rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
            try
            {
                CPBlockBaseClass block = cp.BlockNew();
                //
                cs.Open( "Forums", "id=" + forumId,"",true,"",1,1 );
                if ( cs.OK() ) 
                {
                    forumName = cs.GetText("name");
                    forumCopy = cs.GetText("forumCopy");
                    if (forumCopy.Length < 20)
                    {
                        forumCopy = cs.GetText("forumOverview");
                    }
                }
                cs.Close();
                //
                if (cp.Doc.GetProperty(rnSourceForm,zs)!=formIdNewThread.ToString())
                {
                    //
                    // prepopulate the form from the person's member record
                    //
                    cs.Open("people", "id=" + cp.User.Id,"",true,"",1,1);
                    cp.Doc.SetProperty("foInputName",cs.GetText("name"));
                    cp.Doc.SetProperty("foInputTitle",cs.GetText("title"));
                    company = cs.GetText( "organizationId" );
                    if ( company=="")
                    {
                        company = cs.GetText( "company" );   
                    }
                    cp.Doc.SetProperty("foInputOrg",company);
                    cs.Close();
                }
                //
                block.OpenLayout("forums - new thread view");
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                //
                block.SetInner(".foBreadCrumb", "<a href=\"?" + qs + "\">Forums</a>&nbsp;»&nbsp;" + forumName );
                block.SetInner(".foForumTitle", forumName);
                block.SetInner(".foHeadForumName", forumName);
                block.SetInner(".foBodyTitleForumName", forumName);
                block.SetInner(".foForumCopy", forumCopy);
                //
                block.SetInner(".foInputThreadTitle", "<input type=\"text\" name=\"foInputThreadTitle\" value=\"" + cp.Doc.GetProperty("foInputThreadTitle",zs) + "\">");
                block.SetInner(".foInputName", "<input type=\"text\" name=\"foInputName\" value=\"" + cp.Doc.GetProperty("foInputName", zs) + "\">");
                block.SetInner(".foInputTitle", "<input type=\"text\" name=\"foInputTitle\" value=\"" + cp.Doc.GetProperty("foInputTitle", zs) + "\">");
                block.SetInner(".foInputOrg", "<input type=\"text\" name=\"foInputOrg\" value=\"" + cp.Doc.GetProperty("foInputOrg", zs) + "\">");
                block.SetInner(".foTextAreaPost", encodeHtml(cp,cp.Doc.GetProperty("foTextAreaPost", zs)));
                block.SetInner(".foInputFile", "<input type=\"file\" name=\"foInputFile\">");
                block.SetOuter(".foSourceForm", cp.Html.Hidden(rnSourceForm, formIdNewThread.ToString(), "", ""));
                //
                int createKey = random.Next(0, 2147483647);
                block.SetOuter(".foCreateKey", cp.Html.Hidden(rnCreateKey, createKey.ToString(), "", ""));
                //
                s = block.GetHtml();
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdNewThread.ToString(), true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, forumId.ToString(), true);
                s = s.Replace("$formAction$", "?" + qs + "&requestBinary=1");
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e ,"getPostList Trap");
            }
            return s;
        }
        //
        // ===============================================================================================
        // get New Post
        // ===============================================================================================
        //
        private string getNewPost(CPBaseClass cp, int threadId)
        {
            string s = "";
            string forumName = "";
            //string forumCopy = "";
            string qs;
            string rqs = cp.Doc.RefreshQueryString;
            string zs = "";
            CPCSBaseClass cs = cp.CSNew();
            //string company = "";
            string sql;
            string copy;
            int forumId = 0;
            string threadName = "";
            string threadCopy = "";
            Random random = new Random();
            //
            rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
            //
            try
            {
                CPBlockBaseClass block = cp.BlockNew();
                //
                sql = "select f.id as forumId,f.name as forumName, t.name as threadName, t.copy as threadCopy from ccforumThreads t left join ccForums f on f.id=t.forumId where t.id=" + threadId;
                cs.OpenSQL( sql );
                if (cs.OK())
                {
                    forumName = cs.GetText("forumName");
                    forumId = cs.GetInteger("forumId");
                    threadName = cs.GetText("threadName");
                    threadCopy = cs.GetText("threadCopy");
                }
                cs.Close();
                //
                block.OpenLayout("forums - new post view");
                qs = rqs;
                //
                copy = "<a href=\"?" + qs + "\">Forums</a>";
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, forumId.ToString(), true);
                copy += "&nbsp;»&nbsp;<a href=\"?" + qs + "\">" + forumName + "</a>";
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, threadId.ToString(), true);
                copy += "&nbsp;»&nbsp;<a href=\"?" + qs + "\">" + threadName + "</a>";
                block.SetInner(".foBreadCrumb", copy );
                //
                block.SetInner(".foThreadTitle", threadName);
                block.SetInner(".foThreadCopy", threadCopy);
                block.SetInner(".foHeadThreadName", threadName);
                block.SetInner(".foBodyTitleThreadName", threadName);
                block.SetInner(".foInputName", cp.User.Name);
                string testString = cp.Doc.GetProperty("foTextAreaPost", zs);
                testString = encodeHtml(cp,testString);
                // - for CP that returns null if source is empty -- testString = cp.Utils.encodeHtml(testString); 
                block.SetInner(".foTextAreaPost", testString);
                block.SetInner(".foInputFile", "<input type=\"file\" name=\"foInputFile\">");
                block.SetOuter(".foSourceForm", cp.Html.Hidden(rnSourceForm, formIdNewPost.ToString(), "", ""));
                //
                int createKey = random.Next(0, 2147483647);
                block.SetOuter(".foCreateKey", cp.Html.Hidden(rnCreateKey, createKey.ToString(), "", ""));
                //
                s = block.GetHtml();
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, threadId.ToString(), true);
                s = s.Replace("$formAction$", "?" + qs + "&requestBinary=1");
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "getPostList Trap");
            }
            return s;
        }
        //
        // ===============================================================================================
        // process new post form
        // ===============================================================================================
        //
        private int processNewPost(CPBaseClass cp, int threadId)
        {
            CPCSBaseClass cs = cp.CSNew();
            int nextFormId = formIdNewThread;
            string sql = "";
            int postId = 0;
            int createKey = 0;
            bool blockForm = false;
            //
            nextFormId = formIdNewPost;
            if (cp.Doc.GetProperty(rnNewThreadPost, "") == "")
            {
                cp.UserError.Add("You cannot enter an empty post.");
            }
            if (cp.UserError.OK())
            {
                //
                // test for re-submit
                //
                createKey = cp.Utils.EncodeInteger(cp.Doc.GetProperty(rnCreateKey, ""));
                if (createKey != 0)
                {
                    blockForm = cs.Open("forum posts", "createKey=" + createKey.ToString(), "", true, "", 1, 1);
                    cs.Close();

                }
                if (!blockForm)
                {
                    //
                    // save thread
                    //
                    cs.Insert("forum posts");
                    postId = cs.GetInteger("id");
                    cs.SetField("createKey", createKey.ToString());
                    cs.SetField("name", cp.User.Name + " at " + DateTime.Now.ToString());
                    cs.SetFormInput("copy", "foTextAreaPost");
                    cs.SetField("threadId", threadId.ToString());
                    cs.Close();
                    //
                    // save uploaded file
                    //
                    if (cp.Doc.GetProperty("foInputFile", "") != "")
                    {
                        cs.Insert("forum files");
                        cs.SetFormInput("name", "foInputFile");
                        cs.SetFormInput("filename", "foInputFile");
                        cs.SetField("forumPostId", postId.ToString());
                        cs.Close();
                    }
                    cp.Doc.SetProperty("recordId", postId.ToString());
                    postHousekeepClass postHousekeep = new postHousekeepClass();
                    postHousekeep.Execute(cp);
                }
                //
                nextFormId = formIdPostList;
            }
            sql = "select count(id) as cnt from ccforumPosts where threadId=" + threadId;
            if (cs.OpenSQL(sql))
            {
                cp.Db.ExecuteSQL("update ccforumthreads set replyCnt=" + cs.GetInteger("cnt").ToString() + " where id=" + threadId, "", "1", "1", "1");
            }
            cs.Close();
            //
            return nextFormId;
        }
        //
        // ===============================================================================================
        // get Login
        // ===============================================================================================
        //
        private string getLogin(CPBaseClass cp, int formId, int forumId, int threadId )
        {
            //string copy = "";
            string qs = "";
            string rqs = cp.Doc.RefreshQueryString;
            string s = "<p>Login Form</p>";
            int loginAddonId;
            string loginForm;
            //
            rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
            rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
            //
            try
            {
                if (cp.Utils.EncodeBoolean(cp.Doc.GetProperty("register", "")))
                {
                    s = getRegister(cp, formIdThreadList, forumId, 0);
                }
                else
                {

                    CPBlockBaseClass block = cp.BlockNew();
                    //
                    block.OpenLayout("forums - login view");
                    ////
                    //copy = "<a href=\"?" + qs + "\">Forums</a>";
                    //qs = cp.Utils.ModifyQueryString(qs, rnForumId, forumId.ToString(), true);
                    //copy += "&nbsp;»&nbsp;<a href=\"?" + qs + "\">" + forumName + "</a>";
                    //qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                    //qs = cp.Utils.ModifyQueryString(qs, rnThreadId, threadId.ToString(), true);
                    //copy += "&nbsp;»&nbsp;<a href=\"?" + qs + "\">" + threadName + "</a>";
                    ////block.SetInner(".foBreadCrumb", copy);
                    ////
                    //
                    // if the send password for was submitted, replace the message
                    //
                    if (cp.Doc.GetProperty(rnIntercept, "") == "password")
                    {
                        block.SetInner(".foLoginMessage", cp.UserError.GetList());
                        //if (cp.UserError.OK())
                        //{
                        //}
                        //else
                        //{
                        //    block.SetInner(".foLoginMessage", "<p>Your username and passwordTo contribute to the forum you must log in. If you do not have an account, please use the register link below.</p>");
                        //}
                    }
                    loginAddonId = cp.Site.GetInteger("LOGIN PAGE ADDONID", "0");
                    if (loginAddonId != 0)
                    {
                        loginForm = cp.Utils.ExecuteAddon(loginAddonId.ToString());
                        if (loginForm == "")
                        {
                            cp.Response.Redirect("?" + cp.Doc.RefreshQueryString + "#");
                        }
                        else
                        {
                            block.SetInner("#foLoginPasswordCell", loginForm);
                        }
                    }
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdForumList.ToString(), true);
                    qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", false);
                    block.SetOuter(".foCancelButton", "<a href=\"?" + qs + "\">Cancel</a>");
                    s = block.GetHtml();
                    qs = rqs;
                    qs = cp.Utils.ModifyQueryString(qs, rnFormId, formId.ToString(), true);
                    qs = cp.Utils.ModifyQueryString(qs, rnThreadId, threadId.ToString(), true);
                    qs = cp.Utils.ModifyQueryString(qs, rnForumId, forumId.ToString(), true);
                    s = s.Replace("$formAction$", "?" + qs + "&requestBinary=1");
                    qs = cp.Utils.ModifyQueryString(qs, "register", "true", true);
                    s = s.Replace("$registerLink$", "?" + qs);
                }
                //
                // 
                //
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "method Trap");
            }
            return s;
        }
        //
        // ===============================================================================================
        // process Login
        // ===============================================================================================
        //
        private int processLogin(CPBaseClass cp)
        {
            try
            {
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "method Trap");
            }
            return formIdForumList;
        }
        //
        // ===============================================================================================
        // get Register
        // ===============================================================================================
        //
        private string getRegister(CPBaseClass cp, int formId, int forumId, int threadId)
        {
            //string copy = "";
            string qs = "";
            string rqs = cp.Doc.RefreshQueryString;
            string s = "<p>Login Form</p>";
            CPBlockBaseClass block = cp.BlockNew();
            try
            {
                rqs = cp.Utils.ModifyQueryString(rqs, rnFormId, "", false);
                rqs = cp.Utils.ModifyQueryString(rqs, rnForumId, "", false);
                rqs = cp.Utils.ModifyQueryString(rqs, rnThreadId, "", false);
                rqs = cp.Utils.ModifyQueryString(rqs, rnIntercept, "", false);
                //
                block.OpenLayout("forums - register view");
                //
                // posible error messages
                //
                if (!cp.UserError.OK())
                {
                    block.SetInner(".foRegisterMessage", cp.UserError.GetList());
                }
                //
                block.SetInner(".foRegisterFirst .foInput", "<input type=\"text\" name=\"foInputFirst\" value=\"" + cp.Doc.GetProperty("foInputFirst", "") + "\">");
                block.SetInner(".foRegisterLast .foInput", "<input type=\"text\" name=\"foInputLast\" value=\"" + cp.Doc.GetProperty("foInputLast", "") + "\">");
                block.SetInner(".foRegisterTitle .foInput", "<input type=\"text\" name=\"foInputTitle\" value=\"" + cp.Doc.GetProperty("foInputTitle", "") + "\">");
                block.SetInner(".foRegisterCity .foInput", "<input type=\"text\" name=\"foInputCity\" value=\"" + cp.Doc.GetProperty("foInputCity", "") + "\">");
                block.SetInner(".foRegisterState .foInput", "<input type=\"text\" name=\"foInputState\" value=\"" + cp.Doc.GetProperty("foInputState", "") + "\">");
                block.SetInner(".foRegisterEmail .foInput", "<input type=\"text\" name=\"foInputEmail\" value=\"" + cp.Doc.GetProperty("foInputEmail", "") + "\">");
                block.SetInner(".foRegisterPassword .foInput", "<input type=\"password\" name=\"foInputPassword\" value=\"" + cp.Doc.GetProperty("foInputPassword", "") + "\">");
                block.SetInner(".foRegisterConfirm .foInput", "<input type=\"password\" name=\"foInputConfirm\" value=\"" + cp.Doc.GetProperty("foInputConfirm", "") + "\">");
                block.SetInner(".foRegisterNickname .foInput", "<input type=\"text\" name=\"foInputNickname\" value=\"" + cp.Doc.GetProperty("foInputNickname", "") + "\">");
                if ((cp.Site.Name == "aasa") | (cp.Site.Name == "staging-forum"))
                {
                    block.SetInner(".foRegisterDistrict .foInput", "<input type=\"text\" name=\"foInputDistrict\" value=\"" + cp.Doc.GetProperty("foInputDistrict", "") + "\">");
                }
                else
                {
                    block.SetOuter(".foRegisterDistrict", "");
                }
                //
                s = block.GetHtml();
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, formId.ToString(), true);
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, threadId.ToString(), true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, forumId.ToString(), true);
                qs = cp.Utils.ModifyQueryString(qs, "register", "true", true);
                s = s.Replace("$formAction$", "?" + qs + "&requestBinary=1");
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "getRegister Trap");
            }
            return s;
        }
        //
        // ===============================================================================================
        // process Register
        //  returns true if all went OK, false means there was a problem
        // ===============================================================================================
        //
        private bool processRegister(CPBaseClass cp)
        {
            bool returnValue = false;
            CPCSBaseClass cs = cp.CSNew();
            bool isAasa = false;
            string email = "";
            try
            {
                email = cp.Doc.GetProperty("foInputEmail", "");
                isAasa = ((cp.Site.Name == "aasa") | (cp.Site.Name == "staging-forum"));
                //
                // check required fields
                //
                if (cp.Doc.GetProperty("foInputFirst", "") == "") 
                {
                    cp.UserError.Add("First name is required.");
                }
                else if (cp.Doc.GetProperty("foInputLast", "") == "")
                {
                    cp.UserError.Add("Last name is required.");
                }
                else if ((isAasa) & (cp.Doc.GetProperty("foInputDistrict", "") == ""))
                {
                    cp.UserError.Add("School District is required.");
                }
                else if (email == "")
                {
                    cp.UserError.Add("Email is required.");
                }
                else if (cp.Doc.GetProperty("foInputPassword", "") != cp.Doc.GetProperty("foInputConfirm", ""))
                {
                    cp.UserError.Add("The password and password confirm fields did not match.");
                }
                else
                {
                    cp.User.Logout();
                    if (cs.Open("people", "(username=" + cp.Db.EncodeSQLText(email) + ")or(email=" + cp.Db.EncodeSQLText(email) + ")", "", true, "", 1, 1)) 
                    {
                        cp.UserError.Add( "The email you entered is in use by another account." );
                    }
                    cs.Close();
                    if (cp.UserError.OK())
                    {
                        if (cs.Open("people", "id=" + cp.User.Id, "", true, "", 1, 1))
                        {
                            cs.SetField("name", cp.Doc.GetProperty("foInputFirst", "") + " " + cp.Doc.GetProperty("foInputLast", ""));
                            cs.SetField("firstName", cp.Doc.GetProperty("foInputFirst", ""));
                            cs.SetField("lastName", cp.Doc.GetProperty("foInputLast", ""));
                            cs.SetField("title", cp.Doc.GetProperty("foInputTitle", ""));
                            cs.SetField("city", cp.Doc.GetProperty("foInputCity", ""));
                            cs.SetField("state", cp.Doc.GetProperty("foInputState", ""));
                            cs.SetField("email", email);
                            cs.SetField("username", email);
                            cs.SetField("password", cp.Doc.GetProperty("foInputPassword", ""));
                            cs.SetField("nickname", cp.Doc.GetProperty("foInputNickname", ""));
                            if (isAasa)
                            {
                                cs.SetField("schoolDistrict", cp.Doc.GetProperty("foInputDistrict", ""));
                            }
                        }
                        cs.Close();
                        cp.User.LoginByID(cp.User.Id.ToString(), false);
                        returnValue = true;
                    }
                }

            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "method Trap");
            }
            return returnValue;
        }
        //
        // ===============================================================================================
        //
        // ===============================================================================================
        //
        private string getNewForum(CPBaseClass cp)
        {
            //const string foPrepopulateFlag = "foPrepopulateFlag";
            string s = "";
            //string forumName = "";
            //string forumCopy = "";
            string qs;
            string rqs = cp.Doc.RefreshQueryString;
            string zs = "";
            CPCSBaseClass cs = cp.CSNew();
            //string company = "";
            CPBlockBaseClass block = cp.BlockNew();
            CPBlockBaseClass li = cp.BlockNew();
            string list = "";
            int ptr = 0;
            //int groupId;
            string groupCaption = "";
            string copy = "";
            Random random = new Random();
            //
            try
            {
                //
                block.OpenLayout("forums - new forum view");
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, "", true);
                //
                block.SetInner(".foBreadCrumb", "<a href=\"?" + qs + "\">Forums</a>");
                ////
                block.SetInner(".foInputTitle", "<input type=\"text\" name=\"foInputTitle\" value=\"" + cp.Doc.GetProperty("foInputTitle", zs) + "\">");
                block.SetInner(".foInputDescription", encodeHtml(cp,cp.Doc.GetProperty("foInputDescription", zs)));
                block.SetOuter(".foInputModerator", cp.Html.SelectContent("foModeratorGroupid", cp.Doc.GetProperty("foModeratorGroupid", zs), "groups", "", "No Moderator Group", "", ""));
                block.SetOuter(".foInputBlock", cp.Html.CheckBox("foInputBlock", cp.Utils.EncodeBoolean(cp.Doc.GetProperty("foInputBlock", "")), "", "foInputBlock"));
                block.SetOuter(".foSourceForm", cp.Html.Hidden(rnSourceForm, formIdNewForum.ToString(), "", ""));
                //
                li.Load( block.GetOuter(".foInputAllowGroupsItem"));
                if ( !cs.Open( "groups","","caption",true,"caption,name,id",100,1 ) )
                {
                    block.SetOuter("foInputAllowGroupsList", "");
                }
                else
                {
                    while ( cs.OK()) 
                    {
                        groupCaption = "";
                        groupCaption = cs.GetText( "caption" );
                        if ( groupCaption=="")
                        {
                            groupCaption = cs.GetText( "name" );
                        }
                        li.SetInner(".foCaption", groupCaption);
                        copy = ""
                            + cp.Html.CheckBox("foGroup" + ptr, true, "", "")
                            + cp.Html.Hidden("foGroupId" + ptr, cs.GetInteger("id").ToString(), "", "")
                            + "";
                        li.SetInner(".foGroupInput", copy);
                        list += li.GetHtml();
                        ptr += 1;
                        cs.GoNext();
                    }
                    list += cp.Html.Hidden("foGroupCnt", ptr.ToString(), "", "");
                    block.SetInner(".foInputAllowGroupsList", list);
                }
                //
                int createKey = random.Next(0, 2147483647);
                block.SetOuter(".foCreateKey", cp.Html.Hidden(rnCreateKey, createKey.ToString(), "", ""));
                //
                s = block.GetHtml();
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdNewForum.ToString(), true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnIntercept, "", true);
                s = s.Replace("$formAction$", "?" + qs + "&requestBinary=1");
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "getPostList Trap");
            }
            return s;
        }
        //
        // ===============================================================================================
        //
        // ===============================================================================================
        //
        private int processNewForum(CPBaseClass cp)
        {
            int nextFormId = formIdNewForum;
            CPCSBaseClass cs = cp.CSNew();
            int forumId = 0;
            int ptr = 0;
            int cnt = 0;
            int GroupId = 0;
            int createKey = 0;
            bool blockForm = false;
            //
            try
            {
                //
                // check required fields
                //
                if (cp.Doc.GetProperty("foInputTitle", "") == "")
                {
                    cp.UserError.Add("A Title is required.");
                }
                else
                {
                    //
                    // test for re-submit
                    //
                    createKey = cp.Utils.EncodeInteger(cp.Doc.GetProperty(rnCreateKey, ""));
                    if (createKey != 0)
                    {
                        blockForm = cs.Open("forums", "createKey=" + createKey.ToString(), "", true, "", 1, 1);
                        cs.Close();

                    }
                    if (!blockForm)
                    {
                        if (cs.Insert("forums"))
                        {
                            forumId = cs.GetInteger("id");
                            cs.SetField("createKey", createKey.ToString());
                            cs.SetField("name", cp.Doc.GetProperty("foInputTitle", "forum " + forumId.ToString()));
                            //cs.SetField( "caption", cp.Doc.GetProperty( "foInputTitle", "forum " + forumId.ToString()) );
                            cs.SetField("copy", cp.Doc.GetProperty("foInputDescription", ""));
                            cs.SetField("overview", cp.Doc.GetProperty("foInputDescription", ""));
                            cs.SetField("moderatorGroupId", cp.Doc.GetProperty("foModeratorGroupid", "0"));
                            cs.SetField("block", cp.Doc.GetProperty("foInputBlock", "0"));
                            cs.SetField("threads", "0");
                            cs.SetField("posts", "0");
                            cs.SetField("ownerMemberId", cp.User.Id.ToString());

                        }
                        cs.Close();
                        //
                        cnt = cp.Utils.EncodeInteger(cp.Doc.GetProperty("foGroupCnt", "0"));
                        for (ptr = 0; ptr < cnt; ptr++)
                        {
                            if (cp.Utils.EncodeBoolean(cp.Doc.GetProperty("foGroup" + ptr.ToString(), "0")))
                            {
                                GroupId = cp.Utils.EncodeInteger(cp.Doc.GetProperty("foGroupId" + ptr.ToString(), ""));
                                if (cs.Insert("Forum Group Rules"))
                                {
                                    cs.SetField("forumId", GroupId.ToString());
                                    cs.SetField("groupId", GroupId.ToString());
                                }
                                cs.Close();
                            }
                        }
                    }
                    nextFormId = formIdForumList;
                }

            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "NewForum Trap");
            }
            return nextFormId;
        }
        //
        // returns true if this user has access to this forum
        //
        bool userHasAccess(CPBaseClass cp, int forumId)
        {
            bool returnValue = false;
            try 
            {
                string sql;
                CPCSBaseClass cs = cp.CSNew();
                //
                if (forumId==0)
                {
                    returnValue = true;
                }
                else if (cp.User.IsAdmin)
                {
                    returnValue = true;
                }
                else 
                {
                    if (cp.User.IsAuthenticated)
                    {
                        sql = ""
                            + "select f.id"
                            + " from (((ccforums f"
                            + " left join ccforumGroupRules gr on gr.forumId=f.id)"
                            + " left join ccMemberRules mr on mr.groupId=gr.groupId)"
                            + " left join ccmembers m on m.id=mr.memberid)"
                            + " where (f.id=" + forumId + ")"
                            + " and(f.active<>0)"
                            + " and ("
                            +   " (f.block is null)"
                            +   " or(f.block=0)"
                            +   " or(mr.memberid=" + cp.User.Id + ")"
                            + " )"
                            + "";
                    }
                    else
                    {
                        sql = "select f.id"
                            + " from ccforums f"
                            + " where (f.id=" + forumId + ")"
                            + " and(f.active<>0)"
                            + " and((f.block is null)or(f.block=0))"
                            + "";
                    }
                    cs.OpenSQL(sql );
                    returnValue = cs.OK();
                    cs.Close();
                }
            } 
            catch(Exception ex) 
            {
                cp.Site.ErrorReport(ex, "userHasAccess Trap");
            }
            return returnValue;
        }
        //
        //
        //
        private string encodeHtml( CPBaseClass cp, string source)
        {
            string returnValue = source;
            try
            {
                if (source != "")
                {
                    returnValue = cp.Utils.EncodeHTML(source);
                }
            }
            catch (Exception ex)
            {
                cp.Site.ErrorReport(ex, "encodeHtml Trap");
            }
            return returnValue;
        }
        //
        // ===============================================================================================
        //
        // ===============================================================================================
        //
        private int processProfile(CPBaseClass cp)
        {
            int nextFormId = formIdProfile;
            CPCSBaseClass cs = cp.CSNew();
            int cnt;
            int ptr;
            //bool isChecked;
            string sqlCriteria;
            int forumId;
            int blockGroupId = 0;
            bool tryAgain = false;
            //
            try
            {
                if (cp.Site.GetBoolean("Forums - Allow Profile"))
                {
                    blockGroupId = cp.Site.GetInteger("Forums - Profile Block Update Group", "0");
                    if(!cp.User.IsInGroupList(blockGroupId.ToString()))
                    {
                        //
                        // update profile edit fields
                        //
                        //
                        // check required fields
                        //
                        if (cp.Doc.GetProperty("foFirst", "") == "")
                        {
                            cp.UserError.Add("Your first name is required.");
                        }
                        if (cp.Doc.GetProperty("foLast", "") == "")
                        {
                            cp.UserError.Add("Your last name is required.");
                        }
                        if (cp.Doc.GetProperty("foEmail", "") == "")
                        {
                            cp.UserError.Add("Your email address is required.");
                        }
                        if (cp.UserError.OK())
                        {
                            //
                            // test for re-submit
                            //
                            if (cs.Open("people", "id=" + cp.User.Id, "", true, "", 1, 1))
                            {
                                cs.SetField("firstName", cp.Doc.GetText("foFirst", ""));
                                cs.SetField("lastName", cp.Doc.GetText("foLast", ""));
                                cs.SetField("title", cp.Doc.GetText("foTitle", ""));
                                cs.SetField("city", cp.Doc.GetText("foCity", ""));
                                cs.SetField("state", cp.Doc.GetText("foState", ""));
                                cs.SetField("email", cp.Doc.GetText("foEmail", ""));
                                cs.SetField("nickname", cp.Doc.GetText("foNickname", ""));
                            }
                            cs.Close();
                        }
                    }
                }
                if ((cp.UserError.OK())&(cp.Site.GetBoolean("Forums - Allow Notifications")))
                {
                    //
                    // profile Notifications fields
                    //
                    cnt = cp.Doc.GetInteger("foGroupCnt", "0");
                    for (ptr = 0; ptr < cnt; ptr++)
                    {
                        forumId = cp.Doc.GetInteger("foGroupId" + ptr.ToString(), "0");
                        sqlCriteria = "(memberid=" + cp.User.Id.ToString() + ")and(forumid=" + forumId.ToString() + ")";
                        if (cp.Doc.GetBoolean("foGroup" + ptr.ToString(), "false"))
                        {
                            if (!cs.Open("forum notification rules", sqlCriteria, "", true, "", 1, 1))
                            {
                                cs.Close();
                                cs.Insert("forum notification rules");
                                cs.SetField("memberid", cp.User.Id.ToString());
                                cs.SetField("forumid", forumId.ToString());
                            }
                            cs.Close();
                        }
                        else
                        {
                            cp.Db.ExecuteSQL("delete from ccforumnotificationrules where " + sqlCriteria, "", "", "1", "1");
                        }

                    }
                }
                if (cp.UserError.OK())
                {
                    nextFormId = formIdForumList;
                }
                //
            
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "Profile Trap");
            }
            return nextFormId;
        }
        //
        // ===============================================================================================
        //
        // ===============================================================================================
        //
        private string getProfile(CPBaseClass cp)
        {
            string s = "";
            string qs;
            string rqs = cp.Doc.RefreshQueryString;
            CPCSBaseClass cs = cp.CSNew();
            CPBlockBaseClass block = cp.BlockNew();
            CPBlockBaseClass li = cp.BlockNew();
            string list = "";
            int ptr = 0;
            string forumName = "";
            string copy = "";
            Random random = new Random();
            string sql = "";
            //string form = "";
            string notificationForumIdList = "";
            int forumId = 0;
            bool isChecked = false;
            int blockGroupId = 0;
            //string bodyInstructions;
            //
            try
            {
                //
                block.OpenLayout("forums - profile view");
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, "", true);
                //
                block.SetInner(".foBreadCrumb", "<a href=\"?" + qs + "\">Forums</a>");
                block.SetInner("#foHeader", cp.Content.GetCopy("forums - profile instructions", profileInstructions));
                block.SetInner("#foFooter", cp.Content.GetCopy("forums - profile notification instructions", profileNotificationInstructions));
                if (cp.UserError.OK())
                {
                    block.SetOuter(".foErrors", "");
                }
                else
                {
                    block.SetOuter(".foErrors", cp.UserError.GetList());
                }
                //
                if (!cp.Site.GetBoolean("Forums - Allow Profile"))
                {
                    //
                    // disallow profile
                    //
                    block.SetOuter(".foEditFields", "");
                }
                else
                {
                    //
                    // disallow profile
                    //
                    if (cs.Open("people", "id=" + cp.User.Id, "", true, "", 1, 1))
                    {
                        blockGroupId = cp.Site.GetInteger("Forums - Profile Block Update Group");
                        if (cp.User.IsInGroupList(blockGroupId.ToString()))
                        {
                            //
                            // display profile read-only
                            //
                            block.SetInner(".foFormRowFirst .foInput", cs.GetText("firstName") );
                            block.SetInner(".foFormRowLast .foInput", cs.GetText("lastName") );
                            block.SetInner(".foFormRowTitle .foInput", cs.GetText("title") );
                            block.SetInner(".foFormRowCity .foInput", cs.GetText("city") );
                            block.SetInner(".foFormRowState .foInput", cs.GetText("state") );
                            block.SetInner(".foFormRowEmail .foInput",  cs.GetText("email") );
                            block.SetInner(".foFormRowNickname .foInput", cs.GetText("nickname") );
                            //block.SetInner(".foFormRowFirst .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foFirst\" value=\"" + cs.GetText("firstName") + "\">");
                            //block.SetInner(".foFormRowLast .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foLast\" value=\"" + cs.GetText("lastName") + "\">");
                            //block.SetInner(".foFormRowTitle .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foTitle\" value=\"" + cs.GetText("title") + "\">");
                            //block.SetInner(".foFormRowCity .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foCity\" value=\"" + cs.GetText("city") + "\">");
                            //block.SetInner(".foFormRowState .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foState\" value=\"" + cs.GetText("state") + "\">");
                            //block.SetInner(".foFormRowEmail .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foEmail\" value=\"" + cs.GetText("email") + "\">");
                            //block.SetInner(".foFormRowNickname .foInput", "<input readonly=\"readonly\" type=\"text\" name=\"foNickName\" value=\"" + cs.GetText("nickname") + "\">");
                        }
                        else
                        {
                            //
                            // edit profile
                            //
                            block.SetInner(".foFormRowFirst .foInput", "<input type=\"text\" name=\"foFirst\" value=\"" + cs.GetText("firstName") + "\">");
                            block.SetInner(".foFormRowLast .foInput", "<input type=\"text\" name=\"foLast\" value=\"" + cs.GetText("lastName") + "\">");
                            block.SetInner(".foFormRowTitle .foInput", "<input type=\"text\" name=\"foTitle\" value=\"" + cs.GetText("title") + "\">");
                            block.SetInner(".foFormRowCity .foInput", "<input type=\"text\" name=\"foCity\" value=\"" + cs.GetText("city") + "\">");
                            block.SetInner(".foFormRowState .foInput", "<input type=\"text\" name=\"foState\" value=\"" + cs.GetText("state") + "\">");
                            block.SetInner(".foFormRowEmail .foInput", "<input type=\"text\" name=\"foEmail\" value=\"" + cs.GetText("email") + "\">");
                            block.SetInner(".foFormRowNickname .foInput", "<input type=\"text\" name=\"foNickName\" value=\"" + cs.GetText("nickname") + "\">");
                        }
                    }
                    //
                    cs.Close();
                }
                if (!cp.Site.GetBoolean("Forums - Allow Notifications"))
                {
                    //
                    // disallow notifications
                    //
                    block.SetOuter(".foNotificationFields", "");
                }
                else
                {
                    //
                    // notifications
                    //
                    li.Load(block.GetOuter(".foCheckListItem"));
                    //
                    if (cs.OpenSQL("select forumId from ccForumNotificationRules where memberid=" + cp.User.Id))
                    {
                        while (cs.OK())
                        {
                            notificationForumIdList += "," + cs.GetText("forumId");
                            cs.GoNext();
                        }
                        notificationForumIdList = notificationForumIdList + ",";
                    }
                    cs.Close();
                    //
                    if (cp.User.IsAdmin)
                    {
                        sql = " select * from ccforums where (active<>0)";
                    }
                    else
                    {
                        sql = " select f.*"
                            + " from ((ccforums f"
                            + " left join ccforumGroupRules g on g.forumId=f.id)"
                            + " left join ccmemberrules m on m.groupId=g.groupId)"
                            + " where (m.memberId=" + cp.User.Id.ToString() + ")and(f.active<>0)"
                            + " union"
                            + " select f.*"
                            + " from ccforums f"
                            + " where ((f.block is null)or(f.block=0))and(f.active<>0)"
                            + "";
                    }
                    if (!cs.OpenSQL2( sql, "", 999, 1))
                    {
                        block.SetOuter(".foCheckList", "");
                    }
                    else
                    {
                        while (cs.OK())
                        {
                            forumId = cs.GetInteger("id");
                            isChecked = (notificationForumIdList.IndexOf(","+forumId.ToString()+",")>-1);
                            forumName = cs.GetText("name");
                            li.SetInner(".foCaption", forumName);
                            copy = ""
                                + cp.Html.CheckBox("foGroup" + ptr, isChecked, "", "")
                                + cp.Html.Hidden("foGroupId" + ptr, cs.GetInteger("id").ToString(), "", "")
                                + "";
                            li.SetInner(".foInput", copy);
                            list += li.GetHtml();
                            ptr += 1;
                            cs.GoNext();
                        }
                        list += cp.Html.Hidden("foGroupCnt", ptr.ToString(), "", "");
                        block.SetInner(".foCheckList", list);
                    }
                }
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, "", false);
                block.SetOuter("#foButtonCancel", "<a id=\"foButtonCancel\" href=\"?"+qs+"\">Cancel</a>");
                block.SetOuter(".foSourceForm", cp.Html.Hidden(rnSourceForm, formIdProfile.ToString(), "", ""));
                //form = block.GetInner("");
                
                s = block.GetHtml();
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString(qs, rnFormId, formIdProfile.ToString(), true);
                qs = cp.Utils.ModifyQueryString(qs, rnForumId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnThreadId, "", true);
                qs = cp.Utils.ModifyQueryString(qs, rnIntercept, "", true);
                s = s.Replace("$formAction$", "?" + qs + "&requestBinary=1");
            }
            catch (Exception e)
            {
                cp.Site.ErrorReport(e, "getProfile Trap");
            }
            return s;
        }
    }
}
