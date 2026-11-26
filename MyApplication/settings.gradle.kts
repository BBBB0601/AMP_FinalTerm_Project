pluginManagement {
    repositories {
        google {
            content {
                includeGroupByRegex("com\\.android.*")
                includeGroupByRegex("com\\.google.*")
                includeGroupByRegex("androidx.*")
            }
        }
        mavenCentral()
        gradlePluginPortal()
    }
}
plugins {
    id("org.gradle.toolchains.foojay-resolver-convention") version "1.0.0"
}

val unityLibraryDir = listOf(
    file("unityLibrary"),
    file("../Builds/unity_export/unityLibrary")
).firstOrNull { it.exists() } ?: file("../Builds/unity_export/unityLibrary")

dependencyResolutionManagement {
    repositoriesMode.set(RepositoriesMode.PREFER_SETTINGS)
    repositories {
        google()
        mavenCentral()
        flatDir {
            dirs("${unityLibraryDir}/libs")
        }
    }
}

rootProject.name = "My Application"
include(":app")
include(":unityLibrary")
project(":unityLibrary").projectDir = unityLibraryDir

val xrManifestDir = file("${unityLibraryDir}/xrmanifest.androidlib")
if (xrManifestDir.exists()) {
    include(":unityLibrary:xrmanifest.androidlib")
    project(":unityLibrary:xrmanifest.androidlib").projectDir = xrManifestDir
}
