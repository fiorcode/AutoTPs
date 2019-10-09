using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace AutoTPs
{
    public sealed class Driver
    {
        private static Driver instance = null;

        private Driver(IWebDriver driver)
        {
            this.WebDrive = driver;
        }

        public static Driver GetInstance
        {
            get
            {
                if (instance == null) instance = new Driver(new ChromeDriver("."));
                return instance;
            }
        }

        public IWebDriver WebDrive { get; }

    }
}
