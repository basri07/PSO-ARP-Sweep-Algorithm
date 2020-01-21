using System;

namespace GA_ARP_3
{

    public static class Geography
    {
        const double R = 6371000; // m
        const double Rad2Deg = 180.0 / Math.PI;
        const double Deg2Rad = Math.PI / 180.0;


        // Burda 3. ve 4. bölgelerde açılar 360 derece eksik çıktığı için 360 derece eklendi. Bu arada depo koordinatları (X,Y)(0,0) ise geçerli
        public static double AciHesapla(double x, double y)
        {
            //Arctan(-y/x)*2Pi
            double result = Math.Atan2(-1 * y, x) * Rad2Deg;
            result = Math.Abs(result);
            if ((x < 0 && y < 0) || (x > 0 && y < 0))
                result = 360 - result;

            return result;
        }
        // Swap operatörü Form1.cs'de kullanıldı Buradajş metot kullanılmadı
        public static string SwapCharacters(string value, int position1, int position2)
        {
            char[] array = value.ToCharArray();
            char temp = array[position1];
            array[position1] = array[position2];
            array[position2] = temp;
            return new string(array);
        }

    }
}

