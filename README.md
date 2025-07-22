# Lyra Test Automation Suite using GameDriver


This repository contains a C# automated test suite, implemented using GameDriver framework for Lyra Starter Game on Unreal Engine 5.3.

The project contains a set of smoke tests and a custom helper method designed to solve the challenge of controlling and aiming the player character in game enviroment using  Unreal Engine Enhanced Input System.


1. Smoke Test Suite Rundown

Smoke tests are designed to perform a quick validation of a new Lyra game build. The goal is to answer a fundamental question: "Is the build stable enough to perform further, more detailed tests?"`

This suite is structured using independent tests that rely on shared helper methods(`NavigateToMainMenu`,`NavigateToExperienceSelection`,`StartMatch`) to ensure isolation of tests and avoid code duplication. This is the best practice that prevents a single failure from

The tests replicate and verify the usual user path in the following order:

a) `Test01_LaunchAndVerifyMainMenu`: Confirming the game executable launches and the main menu UI loads successfully.
b) `Test02_NavigateToExperienceSelection`: Ensuring primary UI navigation is functional by clicking "Play Lyra" button and waiting for the next screen to appear.
c) `Test03_StartEliminationMatch`: Validating the core gameplay loop by loading into a match. This tests asset loading, player spawning and HUD initialization.
d) `Test04_BasicInGameActions`: Checking that Player Character can execute basic movement command, confirming the input and character control systems are functional.
e) `Test05_AimAtTargetHelper`: Verifying that the custom aiming solution works, providing complex, engine-speficic automation challenges can be overcome.


2. AimAtTarget Helper Method: The Challenge and Solution


The Challenge: Unreal's Enhanced Input System

In newer versions of Unreal Engine, Epic Games introduced new input system called "Enhanced Input System" - EIS for short. It is completely new system that presents challenges for automation and testing input related mechanics. For completing the task I have thought of 2 solutions that I implemented in the project.

Solution 1:
To reliably trigger actions ( in my example Jump) I have added a custom function `TriggerActionByPath` to `LyraPlayerController`. This function allows the test script to directly inject an `InputAction` into EIS.

Breakdown:
a)C# test calls `TriggerActionByPath` via `api.CallMethod` passing path of a `InputAction` asset - In this case "/Game/Input/Actions/IA_Jump.IA_Jump";
b)`TriggerActionByPath` loads and processes it via `InjectInputForAction`

Solution 2:
For the aiming, I initially attempted to just rely on `api.RotateObject()` but it proved sketchy since there was no visual representation of the test. To change that I added a second function `SetPlayerViewRotation` in `LyraPlayerController`.

Breakdown:
a) `LyraTestHelpers.AimAtPoint` method calculates the Euler angles to aim at the target. It is important to convert the Vector to Euler since we need to pass rotaion as Vector3 so GameDriver can convert it to FRotator in Engine.
b) Calling `SetPlayerViewRotation` via `api.CallMethod` providing rotation as an `FVector`
c) `SetPlayerViewRotation` receives `FVector` converts to FRotator and calls native `SetControlRotation` in order to update player's aim. Achieving bypass of EIS.


3. Lyra Project Modifications

In order to make sure that every test works as intended, the following modifications are necessary.
a) Installing the GameDriver Plugin.
	Follow the official guide to setup plugin correctly https://kb.gamedriver.io/getting-started-with-gamedriver-and-unreal-engine.

b) Add custom C++ functions to `LyraPlayerController`
	In order to  enable robust input action tests, `TriggerActionByPath` function must be added to the `LyraPlayerController`

In `LyraPlayerController.h` add:

//Necessary for injecting input to trigger EIS
UFUNCTION(BlueprintCallable, Category = "Automation")
void TriggerInputActionByPath(const FString\& ActionPath, FVector Value = FVector(1.0, 0, 0));
//Necessary to determine player's rotation
UFUNCTION(BlueprintCallable, Category = "Automation")
void SetPlayerViewRotation(FVector EulerRotation);


In `LyraPlayerController.cpp` add:

//Necessary includes to add at the top of the .cpp file
#include "EnhancedInputSubsystems.h"
#include "InputMappingContext.h"
#include "EnhancedPlayerInput.h"
#include "InputAction.h"


void ALyraPlayerController::TriggerActionByPath(const FString& ActionPath)
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

!Recompile the Lyra Project after adding this code!
 

4. How to configure and run tests

a) Clone this repository to your local machine
b) Open the Lyra project and add the C++ functions to `LyraPlayerController` as described in the section above, and recompile the project.
c) Open the C# `LyraGameDriverTests.sln` test project in Visual Studio 2022

Option 1: Running tests in packaged build
a) In Unreal Editor, package Lyra Game
b) In `LyraSmokeTests.cs` ensure that `[OneTimeSetUp]` and `[OneTimeTearDown]` is uncommented
c) Update `executablePath` to your `LyraGame.exe` file location.
d) IT IS VERY IMPORTANT TO ADD GAMEDRIVER LICENSE (gdio.license) IN \LyraStarterGame\Content\GameDriver` - Create a folder "GameDriver" if it's not there
e) Build `LyraSmokeTests.cs` solution
f) Open Test Explorer and run the tests. It should automatically launch the game, perform tests and close the process.

Option2: Running tests in Editor
a) In `LyraSmokeTests.cs` make sure that `//In Editor` is uncommented and `//Packaged build` section is commented out
b) In Editor open `L_LyraFrontEnd` map
c) Press `Play` in editor.
d) Build `LyraSmokeTests.cs` solution
e) Open Test Explorer and run the tests

Disclaimer when working within Editor:
Due to the fact that tests are working at the speed of the computer. Sometimes there might be the case of flaky tests. To solve the issue of timings and loadings I have increased the waiting times to make tests a little more patient. It's not the best method but allows for safe and more reliable testing results. Please look for  `NavigateToExperienceSelection` helper method and "//If in Editor" and uncomment longer `Thread.Sleep()`. If the sleep is too short before `api.ClickObject(MouseButtons.LEFT, StartGameButton, 1);` the test might fail.


Sources:
GameDriver documentation:
https://kb.gamedriver.io/
https://kb.gamedriver.io/tutorial-testing-lyra-with-gamedriver
Epic Games Unreal Engine documentation:
https://dev.epicgames.com/community/snippets/XVP/unreal-engine-simulate-player-input-with-enhanced-input
https://dev.epicgames.com/documentation/en-us/unreal-engine/input?application_version=4.27
NUnit Documentation:
https://docs.nunit.org/articles/nunit/intro.html
