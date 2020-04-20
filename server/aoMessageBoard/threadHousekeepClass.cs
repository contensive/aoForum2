using System;
using System.Collections.Generic;
using System.Text;
using Contensive.BaseClasses;

namespace aoForums2
{
    public class threadHousekeepClass : Contensive.BaseClasses.AddonBaseClass
    {
        //
        // ===============================================================================================
        // performs all housekeeping when a thread record is changed (add,mod,del)
        // ===============================================================================================
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            //int sourceFormId = 0;
            //int formId = 0;
            //int forumId = 0;
            int threadId = 0;
            string s = "";
            string sql = "";
            CPCSBaseClass cs = cp.CSNew();
            //
            threadId = cp.Utils.EncodeInteger( cp.Doc.GetProperty("recordId",""));
            if (threadId == 0)
            {
                //
                // re count 'threads' in all forums
                // 
                sql = "select ccforums.id as forumId"
                    + ",(select count(ccforumthreads.id) from ccforumthreads where forumId=ccForums.id) as threadCnt"
                    + ",(select count(p.id) from ccforumPosts p left join ccforumThreads t on t.id=p.threadid where t.forumId=ccforums.id) as postCnt"
                    + ",(select max(p.id) from ccforumPosts p left join ccforumThreads t on t.id=p.threadid where t.forumId=ccforums.id) as lastPostId"
                    + " from ccforums"
                    + "";
            }
            else
            {
                //
                // update forum for thread provided
                // 
                sql = "select ccforums.id as forumId"
                    + ",(select count(ccforumthreads.id) from ccforumthreads where forumId=ccForums.id) as threadCnt"
                    + ",(select count(p.id) from ccforumPosts p left join ccforumThreads t on t.id=p.threadid where t.forumId=ccforums.id) as postCnt"
                    + ",(select max(p.id) from ccforumPosts p left join ccforumThreads t on t.id=p.threadid where t.forumId=ccforums.id) as lastPostId"
                    + " from ccforums"
                    + " where ccforums.id in (select forumid from ccforumthreads where id=" + threadId + ")"
                    + "";
            }
            //
            if (cs.OpenSQL2(sql, "", 1000, 1))
            {
                while (cs.OK())
                {
                    sql = "update ccforums set threads=" + cs.GetInteger("threadCnt") + ",posts=" + cs.GetInteger("postCnt") + ",lastPostId=" + cs.GetInteger("lastPostId") + " where id=" + cs.GetInteger("forumId");
                    cp.Db.ExecuteSQL(sql, "", "1", "1", "1");
                    cs.GoNext();
                }
            }
            cs.Close();
            //
            return s;
        }
    }

}