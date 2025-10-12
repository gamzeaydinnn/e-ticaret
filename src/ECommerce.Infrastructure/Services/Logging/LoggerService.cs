using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//bu class Kodun çalışması sırasında neler olduğunu takip etmek için log (kayıt) tutar
//Geliştirme ve test aşamasında özellikle hata ayıklama (debugging) için kullanışl
namespace ECommerce.Infrastructure.Services.Logging
{
    public class LoggerService
    {
        public void LogInfo(string message) => Console.WriteLine($"INFO: {message}");
        public void LogError(string message) => Console.WriteLine($"ERROR: {message}");
    }
}
