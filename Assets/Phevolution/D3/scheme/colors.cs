namespace Phevolution
{
    public partial class D3
    {
        public static string[] colors(string specifier)
        {
            var n = specifier.Length / 6;
            var colors = new string[n];
            var i = 0;
            while (i < n)
            {
                colors[i] = $"#{specifier.Substring(i * 6, 6)}";
                i++;
            }
            return colors;
        }
    }
}