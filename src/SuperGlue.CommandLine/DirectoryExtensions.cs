namespace SuperGlue
{
    public static class DirectoryExtensions
    {
        public static void DeleteDirectoryAndChildren(this System.IO.DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles()) 
                file.Delete();

            foreach (var subDirectory in directory.GetDirectories())
                subDirectory.DeleteDirectoryAndChildren();

            directory.Delete();
        }
    }
}