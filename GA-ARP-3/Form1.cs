using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace GA_ARP_3
{
    // https://github.com/basri07/Sweep-Algorithm-Vehicle-Routing-Problem
    // https://github.com/basri07/PSO-ARP-Sweep-Algorithm
    public partial class Form1 : Form

    {
        //listBox1.Items.Add komutları teker teker açıldığında hesaplamaların doğru yapıldığını kontrol edebiliriz.
        public List<Araclar> Araclist = new List<Araclar>();
        public List<Musteri> MusteriListesi = new List<Musteri>();
        public List<Parcalar> ParcaListesi = new List<Parcalar>();
        public Form1()
        {
            InitializeComponent();
        }
        SqlConnection baglanti;
        SqlDataAdapter da;
        DataSet ds;
        SqlCommand komut;
        public object Pozisyon { get; private set; }
        private void Form1_Load(object sender, EventArgs e)
        {
            // TODO: This line of code loads data into the '_GA_ARP_3DataSet2.Müsteriler' table. You can move, or remove it, as needed.
            this.müsterilerTableAdapter3.Fill(this._GA_ARP_3DataSet2.Müsteriler);


            baglanti = new SqlConnection("Data Source = BASRI\\BASRI; Initial Catalog = GA-ARP-3; Integrated Security = True");
            da = new SqlDataAdapter("Select *From Müsteriler", baglanti);
            ds = new DataSet();
            DataTable dt = new DataTable();
            baglanti.Open();
            da.Fill(dt);
            MusteriGridWiew.DataSource = dt;
            baglanti.Close();
            listBox1.Items.Clear();
            //SQL Serverde Bulunan Müşteri Tablosundan Açısı en küçükten başlayarak sıralı bir şelilde Listeye ekleniyor.
            string sql = "SELECT*FROM Müsteriler order by Acılar";
            baglanti.Open();
            komut = new SqlCommand(sql, baglanti);
            SqlDataReader dr = komut.ExecuteReader();
            while (dr.Read())
            {
                Musteri depo = new Musteri();
                depo.ID = Convert.ToInt32(dr[0]);
                depo.X = Convert.ToDouble(dr[1]);
                depo.Y = Convert.ToDouble(dr[2]);
                depo.Talep = Convert.ToInt32(dr[3]);
                depo.Acılar = Convert.ToDouble(dr[4]);
                depo.Gidildimi = Convert.ToBoolean(dr[5]);
                MusteriListesi.Add(depo);
            }
            baglanti.Close();
            //Arac Tablosundan Araç Listesine ekleme yapıldı
            string sql1 = "SELECT*FROM Arac ";
            baglanti.Open();
            komut = new SqlCommand(sql1, baglanti);
            SqlDataReader dr1 = komut.ExecuteReader();
            while (dr1.Read())
            {
                Araclar arac = new Araclar();
                arac.ID = Convert.ToInt32(dr1[0]);
                arac.Kapasite = Convert.ToInt32(dr1[1]);
                arac.Kullanildimi = Convert.ToBoolean(dr1[2]); ;
                Araclist.Add(arac);
            }
            baglanti.Close();
            // listBox1.Items.Add(Convert.ToString(Araclist)
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int MüsteriSayisi = MusteriListesi.Count;
            double[,] Uzaklık = new double[MüsteriSayisi, MüsteriSayisi];
            double[] Sonuç = new double[MüsteriSayisi];
            int i, j;
            double EnİyiSonuç;
            //Toplam Taleple müşteri ağırlıkları hesaplanacak (kullanılmadı farklı yönteme geçildi)
            double ToplamTalep = 0;
            for (i = 1; i < MüsteriSayisi; i++)
            {
                ToplamTalep += MusteriListesi[i].Talep;
            }
            //Müşterilerin birbirlerine olan uzaklığı
            for (i = 0; i < MüsteriSayisi; i++)
                for (j = 0; j < MüsteriSayisi; j++)
                {
                    Uzaklık[i, j] = Math.Pow(Convert.ToDouble(MusteriGridWiew.Rows[i].Cells[1].Value) - Convert.ToDouble(MusteriGridWiew.Rows[j].Cells[1].Value), 2);
                    Uzaklık[i, j] += Math.Pow(Convert.ToDouble(MusteriGridWiew.Rows[i].Cells[2].Value) - Convert.ToDouble(MusteriGridWiew.Rows[j].Cells[2].Value), 2);
                    Uzaklık[i, j] = Math.Sqrt(Uzaklık[i, j]);
                    Uzaklık[i, j] = Math.Ceiling(Uzaklık[i, j]);//Uzaklıklar tam sayıya çevrildi işlem kolaylığı ve El ile rota uzunluğunun kontrolünün kolaylığı için.
                    //listBox1.Items.Add(String.Format("{0}\n {1} \n {2}", i, j,Uzaklık[i, j]));
                }
            #region Parçacık Rotaları Süpürme Algoritması Yöntemi ile Oluşturulu .PARÇACIK SAYISI (MÜŞTERİ SAYISI -1) KADAR FAKAT MANUEL OLARAK EN FAZLA (MÜŞTERİ SAYISI -1) KADAR VERİLEBİLİR.

            int ParcacıkSayısı = MüsteriSayisi - 1;
            int AracSayisi = Araclist.Count;
            int Mus1, Mus2;
            double Talep = 0;
            //Burda Eniyisonucun başlangıç değeri çok büyük bir sayı
            EnİyiSonuç = 8000000000000;
            string EniyiGuzergah;
            EniyiGuzergah = " ";
            int iterasyonsayisi = 200;
            //Global En iyi Pozisyon
            double[] Eniyipozisyon = new double[ParcacıkSayısı];
            //Parçacığın Mevcut pozisyonu
            double[,] Pozisyon = new double[ParcacıkSayısı, MüsteriSayisi - 1];
            //Parçacığın en iyi sonucu (Başlanıgçta her parçacığın iyi sonucu çok büyük bir sayı)
            double[] İyiSonuç = new double[ParcacıkSayısı];
            for (i = 0; i < ParcacıkSayısı; i++)
            {
                İyiSonuç[i] = 80000000000;
            }
            //Parçacığın En iyi pozisyon
            double[,] iyipozisyon = new double[ParcacıkSayısı, ParcacıkSayısı];
            //Max-Min Parçacık Hızı
            double HizMax = AracSayisi;
            double HızMin = -AracSayisi;
            //Atalet Ağırlığı
            double w = 0.765;
            //Pozisyon Ağırlığı;
            double c1 = 1;
            double c2 = 1;
            //Parçacığın hızı
            double[,] ParcacikHizi = new double[ParcacıkSayısı, ParcacıkSayısı];

            //İvme için rasgele düzgün dağılım [0,1] aralığında sayı üretiyoruz
            Random rastgele = new Random();
            Random rasgele = new Random();
            List<Musteri> bireyinMusterileri = MusteriListesi.CloneList().ToList();
            //t döngüsü iterasyon sayısıdır. Manuel olarak elle verilebilir. Benim işlemci gücümün ve yazdığım kodların optimize olmamasından dolayı iterasyon sayıları düşük
            for (int t = 0; t < 20; t++)
            {
                //Parçacıklar oluşuturulur ve Rotalama - Pozisyon -AmaçFonk değerleri hesaplanır.
                //c döngüsü parçacıkların döngüsüdür fakat c =0 iken if şartları sağlanmadığından dolayı else döner. Yani il parçacığın oluşumu else döngülerinin içinde
                for (int c = 0; c < ParcacıkSayısı; c++)
                {
                    //P Poziston ağırlığı için her aracın müşteri sırasını belirler
                    int p;
                    //a'yı burda tanımlamamızın sebebi rota hafızası için
                    int a = 0;
                    //Araçlar yapılan işlemlerden dolayı kapasitesi azaldığı için her parçacık döngüsünde yeniden clonır.
                    List<Araclar> bireyinAraclari = Araclist.CloneList().ToList();
                    for (i = 0; i < ParcacıkSayısı; i++)
                    {
                        // Eklenen Hız ile oluşan  yeni pozisyon değerlerine göre swap insert oparetörleri kullandım. Pozisyon değeri ne kadar yüksekse o kadar işlem.
                        if (t != 0 && Pozisyon[c, i] != Eniyipozisyon[i])
                        {
                            //Pozisyonlar tam sayıya çevrilir ve değeri kadar döngü ve rassal olarak swap - inser operatörleri uygulanır
                            //Parçacık Hızı r1 ve r2 ivme katsayısı
                            double r1 = rastgele.NextDouble();
                            double r2 = rastgele.NextDouble();
                            //Parçacığın hızı hesaplanıyor
                            ParcacikHizi[c, i] = w * Pozisyon[c, i] + c1 * r1 * (Eniyipozisyon[i] - Pozisyon[c, i]) + c2 * r2 * (iyipozisyon[c, i] - Pozisyon[c, i]);
                            ParcacikHizi[c, i] = Math.Ceiling(ParcacikHizi[c, i]);
                            int[] POS = new int [ParcacıkSayısı];
                            //Parçacığın Muhtemel Yeni Pozisyonu hesaplanıyor 
                            Pozisyon[c, i] = Pozisyon[c, i] + ParcacikHizi[c, i];
                            // Yeni Pozisyon Max - Min Hızları aşamaz
                            if (Pozisyon[c, i] > HizMax)
                            {
                                Pozisyon[c, i] = HizMax;
                            }
                            if (Pozisyon[c, i] < HızMin)
                            {
                                Pozisyon[c, i] = HızMin;
                            }
                            //Pozisyona göre Swap insert operatörleri uygulanıyor
                            POS[i] = Convert.ToInt32(Pozisyon[c, i]);
                            int NewPOS = POS[i];
                            Araclist.Swap(i,NewPOS);
                            while(i==NewPOS)
                            {
                                _ = new Araclar();
                                int Rnd = rastgele.Next(0, NewPOS);
                                Araclar insert = bireyinAraclari[Rnd];
                                bireyinAraclari.RemoveAt(Rnd);
                                bireyinAraclari.Add(insert);
                            }
                            break;
                        }
                    }
                    string[] Guzergah = new string[MüsteriSayisi];
                    //Her parçacığın Başlangıç rota uzunluğu sıfırdır.
                    Sonuç[c] = 0;
                    //listBox1.Items.Add(String.Format("{0}", Guzergah));
                    if (c != 0 && c < MüsteriSayisi - 1)
                    {
                        //ilk parçacık açısı en düşük müşteriden başlayarak başlangıç çözümü üretir sonraki parçacık açısı en düşük 2. müşteriden başlayarak süpürür ve böyle devam eder
                        _ = new Musteri();
                        Musteri insert = bireyinMusterileri[1];
                        bireyinMusterileri.RemoveAt(1);
                        bireyinMusterileri.Add(insert);
                        //b araç dönügüsü
                        for (int b = 0; b < bireyinAraclari.Count; b++)
                        {
                            // p yeni araç döngüsüne girdiğinde yeni aracın gitti müşterilerin sırasını belirler .
                            p = 0;
                            if (a != 0 && a < MüsteriSayisi)
                            {
                                Mus1 = bireyinMusterileri[0].ID;
                                Guzergah[c] += "*" + Convert.ToString(Mus1);
                                Mus2 = bireyinMusterileri[a].ID;
                                Sonuç[c] += Uzaklık[Mus1, Mus2];
                            }
                            //a müşteri sayısına eşit olduğu zaman (a değeri fazla gelir ve bir eksik a değeri aslında son müşteridir.) 
                            if (a == MüsteriSayisi)
                            {
                                Mus1 = bireyinMusterileri[a - 1].ID;
                                Mus2 = bireyinMusterileri[0].ID;
                                Guzergah[c] += "*" + Convert.ToString(Mus2);
                                Sonuç[c] += Uzaklık[Mus1, Mus2];
                                //Pozisyonlar tutulurken araç ID'sine göre tutulur. Araç ID'leri her zaman 0'dan başlayarak 1 artar
                                int PozisyonMusID = bireyinMusterileri[a - 1].ID - 1;
                                int u = PozisyonMusID;
                                double x = Convert.ToDouble(b - 1);
                                //P+2 deki sebeb döngünün sonunda p değerinin sıfır alınmasından dolayı eksilik oluşmakta
                                double y = Convert.ToDouble(p + 2);
                                Pozisyon[c, u] = Convert.ToDouble(x + (y * 0.1));
                                // Pozisyonların maks min sınırları geçmemesi sağlandı.
                                if (Pozisyon[c, u] > HizMax)
                                {
                                    Pozisyon[c, u] = HizMax;
                                }
                                if (Pozisyon[c, u] < HızMin)
                                {
                                    Pozisyon[c, u] = HızMin;
                                }
                                //Global Eniyisonuç -Güzergah - Pozisyon
                                for (i = 0; i <= c; i++)
                                {
                                    if (EnİyiSonuç > Sonuç[i])//En iyi sonuç eğer Sonuç'dan büyükse Sonuç[i] en iyi sonuç olur amaç minimizasyon.
                                    {
                                        EnİyiSonuç = Sonuç[i];
                                        EniyiGuzergah = Guzergah[i];//Dolayısıyla En iyi güzergahta [i]. parçacığın güzergahı olur.
                                        for (int r = 0; r < ParcacıkSayısı; r++)
                                        {
                                            Eniyipozisyon[r] = Pozisyon[c, r];
                                        }
                                    }
                                }
                                //Parçacığın En iyi Sonucu-Pozisyonu

                                if (Sonuç[c] < İyiSonuç[c])
                                {
                                    İyiSonuç[c] = Sonuç[c];
                                    for (i = 0; i < ParcacıkSayısı; i++)
                                    {
                                        iyipozisyon[c, i] = Pozisyon[c, i];
                                    }
                                }
                                //Parçacık Hızı r1 ve r2 ivme katsayısı
                                double r1 = rastgele.NextDouble();
                                double r2 = rastgele.NextDouble();
                                for (i = 0; i < ParcacıkSayısı; i++)
                                {
                                    ParcacikHizi[c, i] = w * Pozisyon[c, i] + c1 * r1 * (Eniyipozisyon[i] - Pozisyon[c, i]) + c2 * r2 * (iyipozisyon[c, i] - Pozisyon[c, i]);
                                }

                              
                                listBox1.Items.Clear();
                             /*for (i =0; i<MüsteriSayisi-1;i++)
                               { 
                                    listBox1.Items.Add(String.Format("{0} \n {1}", i, Pozisyon[c,i].ToString()));

                               }
                               for  (i = 0; i<ParcacıkSayısı;i++)
                               {
                                       listBox1.Items.Add(String.Format("{0} \n {1}", i, ParcacikHizi[c,i].ToString()));
                               }
                                listBox1.Items.Add(String.Format("{0}", Guzergah[c]));
                                listBox1.Items.Add(Sonuç[c].ToString());
                                listBox1.Items.Add(İyiSonuç[c].ToString());
                                listBox1.Items.Add(String.Format("{0} \n {1}", "En iyi ROTA ", EniyiGuzergah));
                                listBox1.Items.Add(String.Format("{0}\n {1}", "En iyi SONUÇ", EnİyiSonuç));                      */


                                //i araç sırası ve ID (0-15) 0.aracın İD 15
                                for (i = 0; i < b - 1; i++)
                                {
                                    listBox1.Items.Add(String.Format("{0}.Araç \n {1} ", i, bireyinAraclari[i].ID));
                                }
                                break;
                            }
                            //a müşteri döngüsü başlattı
                            for (a = a; a < MüsteriSayisi; a++)
                            {
                                //b aracının ilk gittiği müşteri için p bir arttırılırdı.
                                p += 1;
                                try
                                {
                                    //bu dögüde kapasite kontrolü ve müşteriye gidilip gidilmediğine aracın kullanılıp kullanılmadığına bakar.
                                    if (bireyinAraclari[b].Kullanildimi == false && bireyinMusterileri[a].Talep <= bireyinAraclari[b].Kapasite && bireyinMusterileri[a].Gidildimi == false)
                                    {

                                        Mus1 = bireyinMusterileri[a].ID;
                                        Talep = bireyinMusterileri[a].Talep;
                                        //Araç Kapasitesinden Gidilen müşteri talebi kadar düşülür
                                        bireyinAraclari[b].Kapasite = bireyinAraclari[b].Kapasite - bireyinMusterileri[a].Talep;
                                        Guzergah[c] += "*" + Convert.ToString(Mus1);

                                        Mus2 = bireyinMusterileri[a + 1].ID;
                                        // bireyinMusterileri[a].Gidildimi = true;

                                        //Bu döngüde 2. müşteriye önceden gidilip gidilemeyeceği kontrol edilerek Sonuç hesaplanır
                                        if (bireyinMusterileri[a + 1].Talep < bireyinAraclari[b].Kapasite)
                                        {
                                            Sonuç[c] += Uzaklık[Mus1, Mus2];
                                        }
                                        // Müşteri Listesinde ID sıfır olan değer depoya ait olduğu için depoya pozisyon uygulanmaması adına if şartı konuldu.
                                        int PozisyonMusID = bireyinMusterileri[a].ID - 1;
                                        if (bireyinMusterileri[a].ID != 0)
                                        {
                                            int u = PozisyonMusID;
                                            double x = Convert.ToDouble(b);
                                            double y = Convert.ToDouble(p);
                                            Pozisyon[c, u] = Convert.ToDouble(x + (y * 0.1));
                                            if (Pozisyon[c, u] > HizMax)
                                            {
                                                Pozisyon[c, u] = HizMax;
                                            }
                                            if (Pozisyon[c, u] < HızMin)
                                            {
                                                Pozisyon[c, u] = HızMin;
                                            }
                                        }


                                    }
                                    else
                                    {
                                        //döngü bitince Yeni araca geçer fakat a değeri kaldığı yerden devam eder.
                                        Mus1 = bireyinMusterileri[a - 1].ID;
                                        Mus2 = bireyinMusterileri[0].ID;
                                        Guzergah[c] += "*" + Convert.ToString(Mus2) + "---";
                                        //bireyinAraclari[b].Kullanildimi = true;
                                        Sonuç[c] += Uzaklık[Mus1, Mus2];
                                        break;
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }

                        }
                    }
                    else
                    {
                        //Bu c = 0 için döner c=0 parçacığının rotalama-pozisyon-FitnessValue gibi değerlerini hesaplar. 
                        //Aslında tek bir döngüde kodlanabilirdi fakat başlarken böyle devam etti ve ekleyince değiştiremedim
                        for (int b = 0; b < bireyinAraclari.Count; b++)
                        {
                            p = 0;
                            if (a != 0 && a < MüsteriSayisi)
                            {
                                Mus1 = bireyinMusterileri[0].ID;
                                Guzergah[c] += "*" + Convert.ToString(Mus1);
                                Mus2 = bireyinMusterileri[a].ID;
                                Sonuç[c] += Uzaklık[Mus1, Mus2];
                            }
                            if (a == MüsteriSayisi)
                            {
                                Mus2 = bireyinMusterileri[0].ID;
                                Guzergah[c] += "*" + Convert.ToString(Mus2);
                                Mus1 = bireyinMusterileri[a - 1].ID;
                                Sonuç[c] += Uzaklık[Mus1, Mus2];
                                // EnİyiSonuç = Sonuç[c];
                                int PozisyonMusID = bireyinMusterileri[a - 1].ID - 1;
                                int u = PozisyonMusID;
                                double x = Convert.ToDouble(b - 1);
                                double y = Convert.ToDouble(p + 2);
                                Pozisyon[c, u] = Convert.ToDouble(x + (y * 0.1));
                                if (Pozisyon[c, u] > HizMax)
                                {
                                    Pozisyon[c, u] = HizMax;
                                }
                                if (Pozisyon[c, u] < HızMin)
                                {
                                    Pozisyon[c, u] = HızMin;
                                }
                                for (i = 0; i <= c; i++)
                                {
                                    if (EnİyiSonuç >= Sonuç[i])
                                    {
                                        EnİyiSonuç = Sonuç[i];
                                        EniyiGuzergah = Guzergah[i];
                                        for (int r = 0; r < ParcacıkSayısı; r++)
                                        {
                                            Eniyipozisyon[r] = Pozisyon[c, r];
                                        }
                                    }
                                }
                                //Parçacığın En iyi Sonucu-Pozisyonu
                                if (Sonuç[c] < İyiSonuç[c])
                                {
                                    İyiSonuç[c] = Sonuç[c];
                                    for (i = 0; i < ParcacıkSayısı; i++)
                                    {
                                        iyipozisyon[c, i] = Pozisyon[c, i];
                                    }
                                }
                                /*  for (i = 0; i < MüsteriSayisi - 1; i++)
                                  {
                                      listBox1.Items.Add(Pozisyon[c, i].ToString());
                                  }
                                  for (i = 0; i < ParcacıkSayısı; i++)
                                  {
                                      listBox1.Items.Add(String.Format("{0} \n {1}", i, Eniyipozisyon[i].ToString()));
                                  }
                                  listBox1.Items.Add(String.Format("{0}", Guzergah[c]));
                                  listBox1.Items.Add(Sonuç[c].ToString());
                                  listBox1.Items.Add(EnİyiSonuç.ToString()); */
                                break;
                            }
                            for (a = a; a < MüsteriSayisi; a++)
                            {
                                p += 1;
                                try
                                {
                                    if (bireyinAraclari[b].Kullanildimi == false && bireyinMusterileri[a].Talep <= bireyinAraclari[b].Kapasite && bireyinMusterileri[a].Gidildimi == false)
                                    {
                                        Mus1 = bireyinMusterileri[a].ID;
                                        Talep = bireyinMusterileri[a].Talep;
                                        bireyinAraclari[b].Kapasite = bireyinAraclari[b].Kapasite - bireyinMusterileri[a].Talep;
                                        Guzergah[c] += "*" + Convert.ToString(Mus1);
                                        Mus2 = bireyinMusterileri[a + 1].ID;
                                        //bireyinMusterileri[a].Gidildimi = true;
                                        int PozisyonMusID = bireyinMusterileri[a].ID - 1;
                                        if (bireyinMusterileri[a + 1].Talep < bireyinAraclari[b].Kapasite)
                                        {
                                            Sonuç[c] += Uzaklık[Mus1, Mus2];
                                        }
                                        if (bireyinMusterileri[a].ID != 0)
                                        {

                                            int u = PozisyonMusID;
                                            double x = Convert.ToDouble(b);
                                            double y = Convert.ToDouble(p);
                                            Pozisyon[c, u] = Convert.ToDouble(x + (y * 0.1));
                                            if (Pozisyon[c, u] > HizMax)
                                            {
                                                Pozisyon[c, u] = HizMax;
                                            }
                                            if (Pozisyon[c, u] < HızMin)
                                            {
                                                Pozisyon[c, u] = HızMin;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //döngü bitince Yeni araca geçer fakat a değeri kaldığı yerden devam eder.
                                        Mus1 = bireyinMusterileri[a - 1].ID;
                                        Mus2 = bireyinMusterileri[0].ID;
                                        Guzergah[c] += "*" + Convert.ToString(Mus2) + "---";
                                        bireyinAraclari[b].Kullanildimi = true;
                                        Sonuç[c] += Uzaklık[Mus1, Mus2];
                                        break;
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
            }
            #endregion
            listBox1.Items.Add(String.Format("{0} \n {1}", "En iyi ROTA ", EniyiGuzergah));
            listBox1.Items.Add(String.Format("{0}\n {1}", "En iyi SONUÇ", EnİyiSonuç));
        }
    }
    #region Liste kopyalama ve Swap işlemleri için gerekli metotlar.
    internal static class Extensions
    {
        public static IList<T> CloneList<T>(this IList<T> list) where T : ICloneable
        {
            return list.Select(item => (T)item.Clone()).ToList();
        }
        public static IList<T> Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }
    }
    #endregion

}
