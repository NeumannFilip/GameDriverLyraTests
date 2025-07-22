# Lyra Test Automation Suite using GameDriver

This repository contains a C\# automated test suite, implemented using the GameDriver framework for the Lyra Starter Game on Unreal Engine 5.3.

The project includes a set of smoke tests and a custom helper method designed to address the challenge of controlling and aiming the player character within the game environment using Unreal Engine's Enhanced Input System.

-----

## 1\. Smoke Test Suite Rundown

Smoke tests are designed to perform a quick validation of a new Lyra game build. The goal is to answer a fundamental question: "Is the build stable enough to perform further, more detailed tests?"

This suite is structured using independent tests that rely on shared helper methods (`MapsToMainMenu`, `MapsToExperienceSelection`, `StartMatch`) to ensure test isolation and avoid code duplication. This best practice prevents a single failure from cascading.

The tests replicate and verify the usual user path in the following order:

1.  `Test01_LaunchAndVerifyMainMenu`: Confirms the game executable launches and the main menu UI loads successfully.
2.  `Test02_NavigateToExperienceSelection`: Ensures primary UI navigation is functional by clicking the "Play Lyra" button and waiting for the next screen to appear.
3.  `Test03_StartEliminationMatch`: Validates the core gameplay loop by loading into a match. This tests asset loading, player spawning, and HUD initialization.
4.  `Test04_BasicInGameActions`: Checks that the Player Character can execute basic movement commands, confirming the input and character control systems are functional.
5.  `Test05_AimAtTargetHelper`: Verifies that the custom aiming solution works, demonstrating that complex, engine-specific automation challenges can be overcome.

-----

## 2\. AimAtTarget Helper Method: The Challenge and Solution

### The Challenge: Unreal's Enhanced Input System

Newer versions of Unreal Engine introduced a new input system called the "Enhanced Input System" (EIS). This completely new system presents challenges for automating and testing input-related mechanics. To overcome this, two solutions were implemented in this project.

### Solution 1: Triggering Actions by Path

To reliably trigger actions (e.g., Jump), a custom function `TriggerActionByPath` was added to `LyraPlayerController`. This function allows the test script to directly inject an `InputAction` into the EIS.

**Breakdown:**

1.  The C\# test calls `TriggerActionByPath` via `api.CallMethod`, passing the path of an `InputAction` asset (e.g., "/Game/Input/Actions/IA\_Jump.IA\_Jump").
2.  `TriggerActionByPath` loads and processes it via `InjectInputForAction`.

### Solution 2: Direct Player View Rotation

For aiming, initially, relying solely on `api.RotateObject()` proved unreliable due to a lack of visual representation during the test. To address this, a second function, `SetPlayerViewRotation`, was added to `LyraPlayerController`.

**Breakdown:**

1.  The `LyraTestHelpers.AimAtPoint` method calculates the Euler angles required to aim at the target. It's crucial to convert the `FVector` to Euler angles since the rotation needs to be passed as a `Vector3` for GameDriver to convert it to an `FRotator` in the engine.
2.  `SetPlayerViewRotation` is called via `api.CallMethod`, providing the rotation as an `FVector`.
3.  `SetPlayerViewRotation` receives the `FVector`, converts it to an `FRotator`, and calls the native `SetControlRotation` to update the player's aim, effectively bypassing the EIS for this specific action.

-----

## 3\. Lyra Project Modifications

To ensure every test works as intended, the following modifications are necessary for your Lyra project:

1.  **Installing the GameDriver Plugin:**
    Follow the official guide to set up the plugin correctly: [https://kb.gamedriver.io/getting-started-with-gamedriver-and-unreal-engine](https://kb.gamedriver.io/getting-started-with-gamedriver-and-unreal-engine)

2.  **Add custom C++ functions to `LyraPlayerController`:**
    To enable robust input action tests, the `TriggerActionByPath` and `SetPlayerViewRotation` functions must be added to the `LyraPlayerController`.

    In `LyraPlayerController.h`, add:

    ```cpp
    //Necessary for injecting input to trigger EIS
    UFUNCTION(BlueprintCallable, Category = "Automation")
    void TriggerInputActionByPath(const FString& ActionPath);

    //Necessary to determine player's rotation
    UFUNCTION(BlueprintCallable, Category = "Automation")
    void SetPlayerViewRotation(FVector EulerRotation);
    ```

    In `LyraPlayerController.cpp`, add:

    ```cpp
    //Necessary includes to add at the top of the .cpp file
    #include "EnhancedInputSubsystems.h"
    #include "InputMappingContext.h"
    #include "EnhancedPlayerInput.h"
    #include "InputAction.h"

    void ALyraPlayerController::TriggerInputActionByPath(const FString& ActionPath)
    {
        UInputAction* LoadedInputAction = LoadObject<UInputAction>(nullptr, *ActionPath);

        if (!LoadedInputAction)
        {
            UE_LOG(LogTemp, Error, TEXT("Automation: Could not find InputAction at path: %s"), *ActionPath);
            return;
        }

        if (UEnhancedPlayerInput* EIP = Cast<UEnhancedPlayerInput>(this->PlayerInput))
        {
            EIP->InjectInputForAction(LoadedInputAction, FInputActionValue(true));
        }
    }

    void ALyraPlayerController::SetPlayerViewRotation(FVector EulerRotation)
    {
        //Convert FVector to FRotator
        FRotator NewRotation = FRotator(EulerRotation.X, EulerRotation.Y, EulerRotation.Z);
        SetControlRotation(NewRotation);
    }
    ```

    **Important:** Recompile the Lyra Project after adding this code\!

-----

## 4\. How to Configure and Run Tests

1.  Clone this repository to your local machine.
2.  Open the Lyra project and add the C++ functions to `LyraPlayerController` as described in the section above, then recompile the project.
3.  Open the C\# `LyraGameDriverTests.sln` test project in Visual Studio 2022.

### Option 1: Running tests in a packaged build

1.  In Unreal Editor, package the Lyra Game.
2.  In `LyraSmokeTests.cs`, ensure that `[OneTimeSetUp]` and `[OneTimeTearDown]` are uncommented.
3.  Update `executablePath` to your `LyraGame.exe` file location.
4.  **IT IS VERY IMPORTANT TO ADD GAMEDRIVER LICENSE (gdio.license) IN `\LyraStarterGame\Content\GameDriver`** - Create a folder "GameDriver" if it's not there.
5.  Build the `LyraSmokeTests.cs` solution.
6.  Open Test Explorer and run the tests. It should automatically launch the game, perform the tests, and close the process.

### Option 2: Running tests in Editor

1.  In `LyraSmokeTests.cs`, make sure that the `//In Editor` section is uncommented and the `//Packaged build` section is commented out.
2.  In the Unreal Editor, open the `L_LyraFrontEnd` map.
3.  Press `Play` in the editor.
4.  Build the `LyraSmokeTests.cs` solution.
5.  Open Test Explorer and run the tests.

**Disclaimer when working within Editor:**
Due to the fact that tests are working at the speed of the computer, sometimes there might be flaky tests. To solve the issue of timings and loadings, the waiting times have been increased to make tests a little more patient. While not the most optimal method, it allows for safer and more reliable testing results. Please look for the `MapsToExperienceSelection` helper method and the "//If in Editor" comment, and uncomment the longer `Thread.Sleep()`. If the sleep is too short before `api.ClickObject(MouseButtons.LEFT, StartGameButton, 1);`, the test might fail.

-----

## Sources

  * **GameDriver documentation:**

      * [https://kb.gamedriver.io/](https://kb.gamedriver.io/)
      * [https://kb.gamedriver.io/tutorial-testing-lyra-with-gamedriver](https://kb.gamedriver.io/tutorial-testing-lyra-with-gamedriver)

  * **Epic Games Unreal Engine documentation:**

      * [https://dev.epicgames.com/community/snippets/XVP/unreal-engine-simulate-player-input-with-enhanced-input](https://dev.epicgames.com/community/snippets/XVP/unreal-engine-simulate-player-input-with-enhanced-input)
      * [https://dev.epicgames.com/documentation/en-us/unreal-engine/input?application\_version=4.27](https://dev.epicgames.com/documentation/en-us/unreal-engine/input?application_version=4.27)

  * **NUnit Documentation:**

      * [https://docs.nunit.org/articles/nunit/intro.html](https://docs.nunit.org/articles/nunit/intro.html)