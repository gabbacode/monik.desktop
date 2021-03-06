﻿using System.Collections.ObjectModel;

namespace MonikDesktop.Common.ModelsApp
{
    public class Instance
    {
        public int    ID     { get; set; }
        public string Name   { get; set; }
        public Source Source { get; set; }

        public ObservableCollection<Metric> Metrics { get; set; }
    }
}