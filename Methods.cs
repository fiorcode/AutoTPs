using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace AutoTPs
{
    class Methods
    {
        public static void EnterText(
            string element,
            string value,
            string elementType)
        {
            if (elementType.ToLower() == "Id") Driver.GetInstance.WebDrive.FindElement(By.Id(element)).SendKeys(value);
            if (elementType == "Name") Driver.GetInstance.WebDrive.FindElement(By.Name(element)).SendKeys(value);
        }

        public static void Click(
            string element,
            string elementType)
        {
            if (elementType == "Id") Driver.GetInstance.WebDrive.FindElement(By.Id(element)).Click();
            if (elementType == "Name") Driver.GetInstance.WebDrive.FindElement(By.Name(element)).Click();
            if (elementType == "ClassName") Driver.GetInstance.WebDrive.FindElement(By.ClassName(element)).Click();
            if (elementType == "HRef") Driver.GetInstance.WebDrive.FindElement(By.CssSelector(element)).Click();
        }

        public static void SelectDropDown(
            string element,
            string elementType,
            string value)
        {
            if (elementType == "Id")
                new SelectElement(Driver.GetInstance.WebDrive.FindElement(By.Id(element))).SelectByValue(value);
            if (elementType == "Name")
                new SelectElement(Driver.GetInstance.WebDrive.FindElement(By.Name(element))).SelectByValue(value);
            if (elementType == "ClassName")
                new SelectElement(Driver.GetInstance.WebDrive.FindElement(By.ClassName(element))).SelectByValue(value);
            if (elementType == "HRef")
                new SelectElement(Driver.GetInstance.WebDrive.FindElement(By.CssSelector(element))).SelectByValue(value);
        }
    }
}
