/** 
 * Thomas Wang's Original Homepage (now down):  http://www.cris.com/~Ttwang/tech/inthash.htm
 * Bob Jenkins' Write Up: http://burtleburtle.net/bob/hash/integer.html
 */
class HashInt
{
    public static int moveRight(int x, int n)
    {
        int mask = 0x7fffffff; //Integer.MAX_VALUE
        for (int i = 0; i < n; i++)
        {
            x >>= 1;
            x &= mask;
        }
        return x;
    }

    public static int hashInt(int x)
    {
        var AA = x | 0;
        AA -= (AA << 6);
        AA ^= moveRight(AA, 17);
        AA -= (AA << 9);
        AA ^= (AA << 4);
        AA -= (AA << 3);
        AA ^= (AA << 10);
        AA ^= moveRight(AA, 15);
        return AA & int.MaxValue;
    }
}
