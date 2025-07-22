using NUnit.Framework;
using gdio.unreal_api;
using gdio.common.objects;
using System.Diagnostics;

namespace LyraGamedriverTests
{
    [TestFixture]
    public class LyraSmokeTests
    {
        private ApiClient api;
        private Process gameProcess;
        private const int DefaultTimeout = 30;

        //Locators
        private const string MainMenuWidget = "//*[contains(@name, 'W_LyraFrontEnd_C')]";
        private const string StartGameButton = MainMenuWidget + "//StartGameButton";
        private const string ExperienceScreenWidget = "//*[contains(@name, 'W_ExperienceSelectionScreen_C')]";
        private const string QuickplayButton = ExperienceScreenWidget + "//QuickplayButton";
        private const string HudLayoutWidget = "//*[contains(@name, 'W_ShooterHUDLayout_C')]";
        private const string ReticleImage = HudLayoutWidget + "//ExtensionPoint_Reticle";
        private const string PlayerController = "//*[contains(@name, 'LyraPlayerController')]";
        private const string JumpInputAction = "/Game/Input/Actions/IA_Jump.IA_Jump";

        [OneTimeSetUp]
        public void TestSetup()
        {
            //In editor
            /*api = new ApiClient();
            api.Connect("localhost");*/

            //Packaged build
            //If working with Packaged Build it's important to provide path to your exe file
            string executablePath = @"C:\Users\mysla\OneDrive\Desktop\LyraGame\Windows\LyraGame.exe";
            gameProcess = Process.Start(executablePath);
            Thread.Sleep(15000); 
            api = new ApiClient();
            api.Connect("localhost");
            
        }

        [OneTimeTearDown]
        public void TestTeardown()
        {
            //If working with Packaged Build - Shut down the program after tests
            foreach (var process in Process.GetProcessesByName("LyraGame"))
            {
                process.Kill();
            }
            gameProcess?.Dispose();
        }

        private void NavigateToExperienceSelection()
        {
            bool menuVisible = api.WaitForObject(MainMenuWidget, DefaultTimeout);
            Assert.IsTrue(menuVisible, "Helper failed: Main Menu is not visible");

            bool buttonVisible = api.WaitForObject(StartGameButton, DefaultTimeout);
            Assert.IsTrue(buttonVisible, "Helper failed: 'Play Lyra' button was not found");

            api.MouseMoveToObject(StartGameButton, 1);
            Thread.Sleep(500);
            //If In Editor
            //Thread.Sleep(5000);
            api.ClickObject(MouseButtons.LEFT, StartGameButton, 1);

            bool selectionScreenReady = api.WaitForObject(QuickplayButton, DefaultTimeout);
            Assert.IsTrue(selectionScreenReady, "Helper failed: Experience selection scree not visible");
        }

        private void StartMatch()
        {
            NavigateToExperienceSelection();

            bool buttonVisible = api.WaitForObject(QuickplayButton, DefaultTimeout);
            Assert.IsTrue(buttonVisible, "Helper failed: 'Quickplay' button was not found");

            api.MouseMoveToObject(QuickplayButton, 1);
            Thread.Sleep(500);
            api.ClickObject(MouseButtons.LEFT, QuickplayButton, 1);

            bool hudVisible = api.WaitForObject(ReticleImage, 60);
            Assert.IsTrue(hudVisible, "Helper failed: In-Game HUD did not appear");
        }

        //     SMOKE TESTS

        [Test, Order(1)]
        public void Test01_LaunchAndVerifyMainMenu()
        {
            bool menuVisible = api.WaitForObject(MainMenuWidget, DefaultTimeout);
            Assert.IsTrue(menuVisible, "Helper failed: Main Menu is not visible");
        }

        [Test, Order(2)]
        public void Test02_NavigateToExperienceSelection()
        {
            NavigateToExperienceSelection();
        }

        [Test, Order(3)]
        public void Test03_StartEliminationMatch()
        {
            StartMatch();
        }

        [Test, Order(4)]
        public void Test04_BasicInGameActions()
        {
            StartMatch();
            Thread.Sleep(400);

            api.WaitForObject(PlayerController, DefaultTimeout);

            var playerPawn = api.GetObjectFieldValue<LiteGameObject>(PlayerController, "Pawn");
            Assert.IsNotNull(playerPawn, "Could not get Pawn from the PlayerController");

            string playerLocator = playerPawn.HierarchyPath;
            Vector3 initialLocation = api.GetObjectPosition(playerLocator);

            //Call custom method added to LyraPlayerController passing locator to JumpAction
            api.CallMethod(PlayerController, "TriggerActionByPath", new object[] { JumpInputAction });
            Thread.Sleep(500);

            Vector3 newLocation = api.GetObjectPosition(playerLocator);
            Assert.Greater(newLocation.z, initialLocation.z, "Player did not jump after triggering IA_Jump");
        }

        [Test, Order(5)]
        public void Test05_AimAtTargetHelper()
        {

            StartMatch();

            //Find Player Camera
            string playerControllerLocator = "//*[contains(@name, 'LyraPlayerController')]";
            api.WaitForObject(playerControllerLocator, DefaultTimeout);
            var playerPawn = api.GetObjectFieldValue<LiteGameObject>(playerControllerLocator, "Pawn");
            Assert.IsNotNull(playerPawn, "Could not get the possessed Pawn");
            string cameraLocator = playerPawn.HierarchyPath + "/fn:component('CameraComponent')";
            api.WaitForObject(cameraLocator, DefaultTimeout);

            Vector3 initialRotation = api.GetObjectRotation(playerControllerLocator);
            //Confirmation of rotation change
            Console.WriteLine($"Initial Rotation: Pitch={initialRotation.x}, Yaw={initialRotation.y}");
            //Target Point
            Vector3 playerPos = api.GetObjectPosition(playerPawn.HierarchyPath);
            Vector3 targetPoint = new Vector3(playerPos.x + 500, playerPos.y + 500, playerPos.z - 200);
            //Call Helper method
            LyraTestHelpers.AimAtPoint(api, cameraLocator, targetPoint);
            Thread.Sleep(2000);

            Vector3 finalRotation = api.GetObjectRotation(playerControllerLocator);
            //Confirmation of rotation change
            Console.WriteLine($"Final Rotation: Pitch={finalRotation.x}, Yaw={finalRotation.y}");

            Assert.AreNotEqual(initialRotation.y, finalRotation.y, "Yaw rotation did not change after calling AimAtPoint");
        }
    }
}
