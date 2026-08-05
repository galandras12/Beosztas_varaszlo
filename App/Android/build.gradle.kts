// Root build file: plugin verzió-deklarációk (a tényleges alkalmazás az app/ modulban van).
plugins {
    id("com.android.application") version "9.3.1" apply false
    id("org.jetbrains.kotlin.android") version "2.2.10" apply false
    // KSP (Kotlin Symbol Processing) - a Room annotációfeldolgozója ezzel fut a régebbi,
    // újabb Kotlin-verziókkal (2.0+) egyre problémásabb kapt helyett.
    id("com.google.devtools.ksp") version "2.2.10-2.0.2" apply false
}
