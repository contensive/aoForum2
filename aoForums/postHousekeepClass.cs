using System;
using System.Collections.Generic;
using System.Text;
using Contensive.BaseClasses;

namespace aoForums2
{
    public class postHousekeepClass : Contensive.BaseClasses.AddonBaseClass
    {
        //
        // ===============================================================================================
        // performs all housekeeping when a post record is changed (add,mod,del)
        // ===============================================================================================
        //
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            //int sourceFormId = 0;
            //int formId = 0;
            //int forumId = 0;
            int threadId = 0;
            int postId = 0;
            string s = "";
            string sql = "";
            CPCSBaseClass cs = cp.CSNew();
            //
            postId = cp.Utils.EncodeInteger(cp.Doc.GetProperty("recordId",""));
            if ( postId==0 )
            {
                //
                // re count posts for all threads
                // 
                sql = "select ccforumThreads.id as threadId"
                    + ",(select count(p.id) from ccforumPosts p where p.threadId=ccforumThreads.id) as postCnt"
                    + ",(select max(p.id) from ccforumPosts p where p.threadId=ccforumThreads.id) as lastPostId"
                    + " from ccforumThreads"
                    + "";
            }
            else
            {
                //
                // recount posts for just the thread effected
                // 
                sql = "select ccforumThreads.id as threadId"
                    + ",(select count(p.id) from ccforumPosts p where p.threadId=ccforumThreads.id) as postCnt"
                    + ",(select max(p.id) from ccforumPosts p where p.threadId=ccforumThreads.id) as lastPostId"
                    + " from ccforumThreads"
                    + " where ccforumThreads.id in (select threadid from ccforumPosts where id=" + postId + ")"
                    + "";
            }
            if (cs.OpenSQL2(sql, "", 1000, 1))
            {
                threadId = cs.GetInteger("threadId");
                while (cs.OK())
                {
                    sql = "update ccforumthreads"
                        + " set replyCnt=" + cs.GetInteger("postCnt") + ""
                        + ",lastPostId=" + cs.GetInteger("lastPostId") + ""
                        + " where id=" + cs.GetInteger("threadId");
                    cp.Db.ExecuteSQL(sql, "", "1", "1", "1");
                    cs.GoNext();
                }
                if (postId != 0)
                {
                    //
                    // this only affected one post, so only housekeep one thread
                    //
                    cp.Doc.SetProperty("recordId", threadId.ToString());
                }
            }
            cs.Close();
            //
            // this effects lastPostId, so housekeep threads also
            //
            threadHousekeepClass threadHousekeep = new threadHousekeepClass();
            threadHousekeep.Execute(cp);
            //
            return s;
        }
    }

}