using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace loadData
{
    class powerBi
    {
        async void listDataSet(){

            var baseAddress = new Uri("https://api.powerbi.com/v1.0/myorg/");

            using (var httpClient = new HttpClient{ BaseAddress = baseAddress })
            {
              using(var response = await httpClient.GetAsync("datasets{?defaultRetentionPolicy}"))
              {
                    string responseData = await response.Content.ReadAsStringAsync();
              }
            }
        }
    }
}
