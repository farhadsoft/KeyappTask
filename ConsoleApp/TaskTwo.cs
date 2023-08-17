using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace ConsoleApp;

public class TaskTwo
{
    public static async Task TaskTwoAsync()
    {
        var appiumOptions = new AppiumOptions();
        appiumOptions.AddAdditionalCapability("platformName", "Android");
        appiumOptions.AddAdditionalCapability("deviceName", "emulator-5554");
        appiumOptions.AddAdditionalCapability("platformVersion", "13.0");
        appiumOptions.AddAdditionalCapability("automationName", "UiAutomator2");
        appiumOptions.AddAdditionalCapability("noReset", "true");

        var _driver = new AndroidDriver<IWebElement>(new Uri("http://127.0.0.1:4723/wd/hub"), appiumOptions, TimeSpan.FromSeconds(180));
        _driver.FindElementByXPath("//android.widget.TextView[@content-desc=\'Chrome\']").Click();

        await Task.Delay(TimeSpan.FromSeconds(3));

        try
        {
            _driver.FindElementById("com.android.chrome:id/terms_accept").Click();
            await Task.Delay(TimeSpan.FromSeconds(1));
            _driver.FindElementById("com.android.chrome:id/negative_button").Click();
            await Task.Delay(TimeSpan.FromSeconds(5));
            _driver.FindElementById("com.android.chrome:id/negative_button").Click();
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        catch (Exception)
        {
            Console.WriteLine("No terms and conditions");
        }

        try
        {
            _driver.FindElementById("com.android.chrome:id/search_box_text").SendKeys("my ip address");
            _driver.PressKeyCode(AndroidKeyCode.Enter);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Something is went wrong: " + ex.Message);
        }

        await Task.Delay(TimeSpan.FromSeconds(3));

        try
        {
            _driver.Context = _driver.Contexts.Last();
            var spanElements = _driver.FindElementsByTagName("span").Where(x => x.Text != string.Empty).Select(x => x.Text).ToArray();

            for (int i = 0; i < spanElements.Length; i++)
            {
                if (spanElements[i] == "Your public IP address")
                {
                    Console.WriteLine(spanElements[i]);
                    Console.WriteLine(spanElements[i - 1]);
                    break;
                }
            }
        }
        catch (Exception)
        {
            Console.WriteLine("No IP address found");
        }
        finally
        {
            _driver.Quit();
            _driver.Dispose();
        }
    }
}
