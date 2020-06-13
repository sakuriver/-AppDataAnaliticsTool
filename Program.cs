using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.Generic;
using OpenQA.Selenium.Remote;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Linq;

namespace AppDataAnaliticsTool
{

    public class AppRankingIphone {
      public string Id { get; set; }
      public int Rank{ get; set; }
    }

    public class AppRankingIphoneMaster { 
      public string Id { get; set; }
      public string Name { get; set; }
      // issue タグ拡張
      public string Genre { get; set; }

    }



    class Program
    {
        static void Main(string[] args)
        {

            // ローカルのマスターデータを取得する

            // マスターデータに存在しないアプリは、新しく出たものなのでデータ情報として格納して通知をする

            string[] dataCategoryNameList = new string[] { 
                "top-grossing-iphone/6014.html",
                "top-paid-iphone/6014.html"
            };
            string baseUrl = "http://topappranking300.appios.net/";
            foreach (var (dataCategoryName, driver) in from string dataCategoryName in dataCategoryNameList
                                                       let driver = new ChromeDriver()
                                                       select (dataCategoryName, driver))
            {
                driver.Navigate().GoToUrl(baseUrl + dataCategoryName);
                Thread.Sleep(12000);
                // ランキングの要素を一覧で取得をする
                var elements = driver.FindElements(By.ClassName("span2"));
                // ジャンル毎の集計結果件数
                var iphoneNewMasters = new List<AppRankingIphoneMaster>();
                var appRankingIphones = new List<AppRankingIphone>();
                //Read
                List<String> iphoneMasterIds = new List<String>();
                var filePath = @"c:\develop\\analitics\\master.json";
                if (System.IO.File.Exists(filePath))
                {
                    using (var fs1 = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bytes = new byte[fs1.Length];
                        int numBytesToRead = (int)fs1.Length;
                        int numBytesRead = 0;
                        fs1.Read(bytes, numBytesRead, numBytesToRead);
                        var result = Encoding.Default.GetString(bytes);
                        Console.Write(result);
                        iphoneNewMasters = JsonConvert.DeserializeObject<List<AppRankingIphoneMaster>>(result);
                        foreach (AppRankingIphoneMaster iphoneNewMaster in iphoneNewMasters)
                        {
                            Console.WriteLine(iphoneNewMaster.Name + " 読み込み結果");
                            iphoneMasterIds.Add(iphoneNewMaster.Id);
                        }

                    }

                }

                for (int i = 0; i < elements.Count; i += 1)
                {
                    // タイトル名と、順位を取得する
                    var rank = i + 1;
                    var titleName = elements[i].FindElements(By.TagName("small"))[0].Text;
                    Console.WriteLine("順位" + rank.ToString() + " タイトル：" + titleName);
                    var aHrefObjects = elements[i].FindElements(By.TagName("a"));
                    if (aHrefObjects.Count == 0)
                    {
                        continue;
                    }
                    // サイト内におけるアプリIdを取得する　人気のあるカテゴリーやランキング集計で利用する
                    var hrefText = aHrefObjects[0].GetAttribute("href");
                    var appId = hrefText.Replace("http://topappranking300.appios.net/apps", "").Replace("/", "");
                    Console.WriteLine("appId:" + appId);

                    // 順位とidがセットで取得できていたら集計用元リストに格納をする
                    AppRankingIphone appRankingIphone = new AppRankingIphone();
                    appRankingIphone.Id = appId;
                    appRankingIphone.Rank = rank;
                    appRankingIphones.Add(appRankingIphone);

                    // マスターデータに存在するファイルであるかを確認する
                    if (iphoneMasterIds.Contains(appId))
                    {
                        Console.Write("既に存在するマスターデータなのでスキップします タイトル名");
                        continue;
                    }

                    var iphoneNewMaster = new AppRankingIphoneMaster();
                    iphoneNewMaster.Id = appId;
                    iphoneNewMaster.Name = titleName;
                    // ジャンルは各会社ごとに編集をして、集計時に利用する
                    iphoneNewMaster.Genre = "";
                    iphoneNewMasters.Add(iphoneNewMaster);

                }

                string category = dataCategoryName.Split('/')[0];
                Console.WriteLine(String.Format("category name : {0} output count : {1} ", category, appRankingIphones.Count.ToString()));
                // 実行した年月の集計情報として保存をする
                DateTime dt = DateTime.Now;
                string dtFormat = dt.ToString("yyyy-MM-dd");
                using (StreamWriter file = File.CreateText(@"c:\develop\\analitics\\iphone_free_ranking\\" + category + "_data_" + dtFormat + ".json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var jsonResult = JsonConvert.SerializeObject(appRankingIphones, Formatting.Indented);
                    Console.Write(jsonResult);
                    file.Write(jsonResult);
                }
                // serialize JSON directly to a file
                using (StreamWriter file = File.CreateText(@"c:\develop\\analitics\\master.json"))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var jsonResult = JsonConvert.SerializeObject(iphoneNewMasters, Formatting.Indented);
                    Console.Write(jsonResult);
                    file.Write(jsonResult);
                }
            }
        }
    }
}
