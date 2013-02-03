using System.Web.Mvc;

// ReSharper disable CheckNamespace
namespace Memeify.Web {

    public class FilterConfig {

        public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
            filters.Add(new HandleErrorAttribute());
        }

    }

}
// ReSharper restore CheckNamespace