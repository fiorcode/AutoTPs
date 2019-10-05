using OpenQA.Selenium;

namespace AutoTPs
{
    class SeleniumMethods
    {
        public static void EnterText(
            IWebDriver driver, 
            string element, 
            string value, 
            string elementType)
        {
            if(elementType.ToLower() == "Id") driver.FindElement(By.Id(element)).SendKeys(value);
            if(elementType == "Name") driver.FindElement(By.Name(element)).SendKeys(value);
        }

        public static void Click(
            IWebDriver driver,
            string element,
            string elementType)
        {
            if (elementType == "Id") driver.FindElement(By.Id(element)).Click();
            if (elementType == "Name") driver.FindElement(By.Name(element)).Click();
            if (elementType == "ClassName") driver.FindElement(By.ClassName(element)).Click();
            if (elementType == "HRef") driver.FindElement(By.CssSelector(element)).Click();
        }
    }
}
