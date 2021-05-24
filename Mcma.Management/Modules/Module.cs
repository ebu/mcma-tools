﻿namespace Mcma.Management.Modules
{
    public class Module
    {
        private Provider _provider;
        private Version _version;
        
        public string Namespace { get; set; }
        
        public string Name { get; set; }

        public string Provider
        {
            get => _provider;
            set => _provider = new Provider(value);
        }

        public string Version
        {
            get => _version.ToString();
            set => _version = Management.Version.Parse(value);
        }
        
        public string DisplayName { get; set; }
        
        public string Description { get; set; }
        
        public string Owner { get; set; }
        
        public ModuleParameter[] InputParameters { get; set; }
        
        public ModuleParameter[] OutputParameters { get; set; }
        
        public string[] Tags { get; set; }
    }
}