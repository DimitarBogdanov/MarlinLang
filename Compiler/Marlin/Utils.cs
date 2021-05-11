/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     Utils.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Parsing;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using TreeGenerator;

namespace Marlin
{
    public class Utils
    {
        public static long CurrentTimeMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static bool FireOrDirExists(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }

        public static void GenerateImage(Node root, string path)
        {
            TreeData.TreeDataTableDataTable table = new();
            AddChildrenRecursively(root, null, table);
            TreeBuilder builder = new(table)
            {
                BoxHeight = 45,
                BoxWidth = 170,
            };
            Image.FromStream(builder.GenerateTree("__ROOT__", ImageFormat.Png)).Save(path);
        }

        public static void DeleteImageIfExists(string path)
        {
            if (!path.EndsWith(".mar.png") || !path.StartsWith(Program.SOURCE_DIR))
                return;

            try
            {
                File.Delete(path);
            }
            catch (Exception) { }
        }

        public unsafe static sbyte* UnsafeStringToSByte(string str)
        {
            fixed (byte* p = Encoding.UTF8.GetBytes(str))
                return (sbyte*)p;
        }

        private static void AddChildrenRecursively(Node node, Node parent, TreeData.TreeDataTableDataTable table)
        {
            table.AddTreeDataTableRow(node.Id, (parent != null ? parent.Id : ""), node.Type.ToString(), node.ToString());
            foreach (Node childNode in node.Children)
                if (childNode != null)
                    AddChildrenRecursively(childNode, node, table);
        }
    }
}
