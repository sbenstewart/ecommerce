﻿using System; 
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace OpenShopVHBackend.Models
{
    public class Document
    {
        [Key]
        public Int32 DocumentId { get; set; }
        public String DocumentCode { get; set; }
        public String CreatedDate { get; set; }
        public String DueDate { get; set; }
        public Double TotalAmount { get; set; }
        public Double PayedAmount { get; set; }
        public Double BalanceDue { get; set; }
        public Int32 ClientId { get; set; }
        public Int32 DocEntry { get; set; }
        public Int32 OverdueDays { get; set; }
        public virtual Client Client { get; set; }
    }
}