using System.Collections.Generic;

namespace Maritime.Core.Models
{
    public class MenuItemModel
    {
        public string Code { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public int Order { get; set; }
        public List<MenuItemModel> Children { get; set; }
        public bool IsMapped { get; set; }
        public string PageName { get; set; }

        public MenuItemModel()
        {
            Children = new List<MenuItemModel>();
            IsMapped = false;
        }
    }
}