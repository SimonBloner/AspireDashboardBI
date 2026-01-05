using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspireDashboardBI.Web
{
    [Table("orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public string CustomerName { get; set; }
    }
}
