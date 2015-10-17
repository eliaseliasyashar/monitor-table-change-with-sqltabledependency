﻿using System.ComponentModel.DataAnnotations.Schema;

namespace TableDependency.OracleClient.IntegrationTest.Model
{
    public class Item
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int qty { get; set; }
    }
}