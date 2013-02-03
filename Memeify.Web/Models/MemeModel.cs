using System.Web;

namespace Memeify.Web.Models {

    public class MemeModel {

        public HttpPostedFileBase Image { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

    }

}