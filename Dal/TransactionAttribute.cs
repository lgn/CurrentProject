using System.Web.Mvc;

namespace Dal
{
    public class TransactionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext) {
            NHibernateSessionStorage.Transaction.Begin();
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext) {
            var currentTransaction = NHibernateSessionStorage.Transaction;
            if (currentTransaction.IsActive) {
                if (filterContext.Exception == null) {
                    currentTransaction.Commit();
                } else {
                    currentTransaction.Rollback();
                }
            }
        }
    }
}