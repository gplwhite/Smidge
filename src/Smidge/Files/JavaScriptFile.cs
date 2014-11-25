﻿using System;

namespace Smidge.Files
{
    public class JavaScriptFile : BasicFile
    {
        public JavaScriptFile()
        {

        }
        public JavaScriptFile(string path)
        {
            FilePath = path;
        }
        public override WebFileType DependencyType
        {
            get { return WebFileType.Js; }
        }
    }
}