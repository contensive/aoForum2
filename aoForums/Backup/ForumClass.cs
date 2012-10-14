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
        //
        private const int formIdForumList = 1;
        private const int formIdThreadList = 2;
        private const int formIdPostList = 3;
        //
        // execute method is the only public
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            int formId = 0;
            int forumId = 0;
            int threadId = 0;
            string s = "";
            //
            //
            //
            formId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnFormId));
            forumId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnForumId));
            threadId = cp.Utils.EncodeInteger(cp.Doc.get_Var(rnThreadId));
            //
            // process current form
            //
            if (formId != 0)
            {
                switch (formId)
                {
                    case formIdForumList:
                        formId = processForumList(cp);
                        break;
                    case formIdThreadList:
                        formId = processThreadList(cp);
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
            // get next form
            //
            switch ( formId ) {
                case formIdPostList:
                    s = getThreadList(cp, threadId );
                    break;
                case formIdThreadList:
                    s = getThreadList(cp, forumId );
                    break;
                default:
                    s = getForumList(cp);
                    break;
                    
            }
            //
            // return result
            //
            return s;
        }
        //
        // process Forum List
        //
        private int processForumList(CPBaseClass cp)
        {
            return formIdForumList;
        }
        //
        // process Thread List
        //
        private int processThreadList(CPBaseClass cp)
        {
            return formIdThreadList;
        }
        //
        // process Post List
        //
        private int processPostList(CPBaseClass cp)
        {
            return formIdPostList;
        }
        //
        // get Forum List
        //
        private string getForumList(CPBaseClass cp)
        {
            CPBlockBaseClass block = cp.BlockNew();
            CPBlockBaseClass listItemOdd = cp.BlockNew();
            CPBlockBaseClass listItemEven = cp.BlockNew();
            CPBlockBaseClass listItem;
            CPCSBaseClass cs = cp.CSNew();
            string imageUrl = "";
            string list = "";
            int ptr = 0;
            string copy = "";
            string sql = "";
            string forumATag = "";
            string postATag = "";
            string qs = "";
            string rqs = "";
            DateTime lastDate;
            //
            block.OpenLayout("forums - forum list view");
            listItemOdd.Load(block.GetOuter(".foItemOdd"));
            listItemEven.Load(block.GetOuter(".foItemEven"));
            list = block.GetOuter(".foHead");
            rqs = cp.Doc.RefreshQueryString;
            //
            sql = ""
                + "select f.id,f.name,f.overview,f.threads,f.posts,p.name as lastTitle,p.dateAdded as lastDate,m.name as authorName,f.imageFilename,f.lastpostid"
                + " from ((ccforums f"
                + " left join ccforumPosts p on p.id=f.lastpostid)"
                + " left join ccmembers m on m.id=p.createdby)"
                + " where (f.active<>0)"
                + " group by f.id,f.name,f.overview,f.threads,f.posts,p.name,p.dateAdded,m.name,f.imageFilename,f.lastpostid"
                + " order by f.posts";
            cs.OpenSQL2(sql,"",10,1);
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
                qs = cp.Utils.ModifyQueryString(rqs, rnForumId, cs.GetText("id"), true);
                forumATag = "<a href=\"?" + qs + "\">";
                qs = cp.Utils.ModifyQueryString(rqs, rnPostId, cs.GetText("lastpostid"), true);
                postATag = "<a href=\"?" + qs + "\">";
                imageUrl = cs.GetText("imageFilename");
                if ( imageUrl == "" ) 
                {
                    imageUrl = "/aoForums/defaultForumIcon.png";
                } else {
                    imageUrl = cp.Site.FilePath + imageUrl;
                }
                listItem.SetInner(".overviewImage", forumATag + "<img src=\"" + imageUrl + "\" width=\"80\" height=\"80\"></a>");
                copy = cs.GetText("name");
                if (copy != "")
                {
                    copy = "<b>" + copy + "</b> - ";
                }
                listItem.SetInner(".overviewCopy", forumATag + copy + cs.GetText("overview") + "</a>" );
                listItem.SetInner(".threads", cs.GetText("threads"));
                listItem.SetInner(".posts", cs.GetText("posts"));
                lastDate = cs.GetDate("lastDate");
                if ( lastDate == DateTime.MinValue) {
                    listItem.SetInner(".lastTitle", "");
                    listItem.SetInner(".authorName", "");
                    listItem.SetInner(".lastDate", "");
                }
                else
                {

                    listItem.SetInner(".lastTitle", postATag + cs.GetText("lastTitle") + "</a>");
                    listItem.SetInner(".authorName", cs.GetText("authorName"));
                    listItem.SetInner(".lastDate", lastDate.ToShortDateString());
                }
                list += listItem.GetHtml();
                ptr += 1;
                cs.GoNext();
            }
            if (list != "")
            {
                block.SetInner(".foForums", list);
            }
            return block.GetHtml();
        }
        //
        // get Forum List
        //
        private string getThreadList(CPBaseClass cp, int forumId )
        {
            CPBlockBaseClass block = cp.BlockNew();
            CPBlockBaseClass listItemOdd = cp.BlockNew();
            CPBlockBaseClass listItemEven = cp.BlockNew();
            CPBlockBaseClass listItem;
            CPCSBaseClass cs = cp.CSNew();
            string imageUrl = "";
            string list = "";
            int ptr = 0;
            string copy = "";
            string sql = "";
            string threadATag = "";
            string postATag = "";
            string qs = "";
            string rqs = "";
            int threadId;
            DateTime lastDate;
            string startedByName = "";
            string breadCrumb = "";
            //
            block.OpenLayout("forums - thread list view");
            listItemOdd.Load(block.GetOuter(".foItemOdd"));
            listItemEven.Load(block.GetOuter(".foItemEven"));
            list = block.GetOuter(".foHead");
            rqs = cp.Doc.RefreshQueryString;
            //
            sql = ""
                + "select t.id,t.forumId,t.name,t.copy,t.createdBy,t.viewCnt,t.replyCnt,t.imageFilename,t.lastpostid"
                + " ,p.dateAdded as lastPostDate"
                + " ,mt.name as startedByName"
                + " ,mp.name as lastPostName"
                + " ,f.name as forumName"
                + " ,f.copy as forumCopy"
                + "  from ((((ccforumThreads t"
                + "  left join ccforums f on f.id=t.forumid)"
                + "  left join ccforumPosts p on p.id=t.lastpostid)"
                + "  left join ccmembers mt on mt.id=p.createdby)"
                + "  left join ccmembers mp on mp.id=p.createdby)"
                + "  where (t.active<>0)and(forumId="+forumId.ToString()+")"
                + "  group by "
                + " t.id,t.forumId,t.name,t.copy,t.createdBy,t.viewCnt,t.replyCnt,t.imageFilename,t.lastpostid"
                + " ,p.dateAdded"
                + " ,mt.name"
                + " ,mp.name"
                + " ,f.name,f.copy"
                + "  order by t.id desc";
            cs.OpenSQL2(sql, "", 10, 1);
            ptr = 0;
            if (!cs.OK())
            {
                list = "";
                qs = rqs;
                qs = cp.Utils.ModifyQueryString( qs, rnFormId, "", true );
                qs = cp.Utils.ModifyQueryString( qs, rnForumId, "", true );
                block.Clear();
                block.Append("<p>The Forum you requested could not be found. Please return to the <a href=\"?" + qs + "\">Forum list</a>.</p>");
            }
            else
            {
                //
                // title over thread list
                //
                qs = rqs;
                qs = cp.Utils.ModifyQueryString( qs, rnFormId, "", true );
                qs = cp.Utils.ModifyQueryString( qs, rnForumId, "", true );
                block.SetInner(".foForumTitle", cs.GetText("forumName"));
                copy = cs.GetText("forumCopy");
                block.SetInner(".foForumCopy", copy);
                block.SetInner(".foBreadCrumb", "<a href=\"?" + qs + "\">Forums</a>&nbsp;»&nbsp;" + cp.Utils.EncodeHTML(cs.GetText("forumName")));
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
                    qs = cp.Utils.ModifyQueryString(rqs, rnThreadId, cs.GetText("id"), true);
                    threadId = cs.GetInteger("id");
                    threadATag = "<a href=\"?" + qs + "\">";
                    imageUrl = cs.GetText("imageFilename");
                    if (imageUrl == "")
                    {
                        imageUrl = "/aoForums/defaultThreadIcon.png";
                    }
                    else
                    {
                        imageUrl = cp.Site.FilePath + imageUrl;
                    }
                    listItem.SetInner(".overviewImage", threadATag + "<img src=\"" + imageUrl + "\" width=\"80\" height=\"80\"></a>");
                    copy = cs.GetText("name");
                    if (copy == "")
                    {
                        copy = "Thread " + threadId.ToString();
                    }
                    copy = "<b>" + copy + "</b>";
                    startedByName = cs.GetText("startedByName");
                    if (startedByName != "")
                    {
                        copy += " - Started By " + startedByName;
                    }
                    listItem.SetInner(".overviewCopy", threadATag + copy + "</a>");
                    //
                    listItem.SetInner(".replies", cp.Utils.EncodeText( cs.GetInteger("replyCnt")));
                    listItem.SetInner(".views", cp.Utils.EncodeText( cs.GetInteger("viewCnt")));
                    //
                    lastDate = cs.GetDate("lastPostDate");
                    if (lastDate == DateTime.MinValue)
                    {
                        listItem.SetInner(".authorName", "");
                        listItem.SetInner(".lastDate", "");
                    }
                    else
                    {
                        listItem.SetInner(".authorName", cs.GetText("lastPostName"));
                        listItem.SetInner(".lastDate", lastDate.ToShortDateString());
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
            return block.GetHtml();
        }
        //
        // get Post List
        //
        private string getPostList(CPBaseClass cp,int threadId)
        {
            return "post list";
        }
    }
}
