plugins {
    alias(libs.plugins.android.application)
    alias(libs.plugins.kotlin.compose)
    alias(libs.plugins.ksp)
}

val configureDebugApiReverse by tasks.registering(Exec::class) {
    group = "development"
    description = "Collega automaticamente l'emulatore all'API SkyLab locale."
    commandLine(
        File(System.getenv("LOCALAPPDATA"), "Android/Sdk/platform-tools/adb.exe").absolutePath,
        "-e",
        "reverse",
        "tcp:5187",
        "tcp:5187"
    )
    isIgnoreExitValue = true
}

tasks.configureEach {
    if (name == "assembleDebug" || name == "installDebug") {
        dependsOn(configureDebugApiReverse)
    }
}

android {
    namespace = "it.skylab.mobile"
    compileSdk {
        version = release(37)
    }

    defaultConfig {
        applicationId = "it.skylab.mobile"
        minSdk = 26
        targetSdk = 37
        versionCode = 6
        versionName = "1.0.4"
        buildConfigField("String", "API_BASE_URL", "\"https://skylab.sigmadata.it\"")

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
    }

    signingConfigs {
        create("distribution") {
            val keyPath = System.getenv("SKYLAB_ANDROID_KEYSTORE")
            if (!keyPath.isNullOrBlank()) {
                storeFile = file(keyPath)
                storePassword = System.getenv("SKYLAB_ANDROID_STORE_PASSWORD")
                keyAlias = "skylab"
                keyPassword = System.getenv("SKYLAB_ANDROID_STORE_PASSWORD")
            }
        }
    }
    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("distribution")
            optimization {
                enable = false
            }
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    buildFeatures {
        compose = true
        buildConfig = true
    }
}

tasks.configureEach {
    if (name == "validateSigningRelease") {
        doFirst {
            check(!System.getenv("SKYLAB_ANDROID_KEYSTORE").isNullOrBlank()) { "Chiave di distribuzione SkyLab non configurata." }
            check(!System.getenv("SKYLAB_ANDROID_STORE_PASSWORD").isNullOrBlank()) { "Password della chiave SkyLab non configurata." }
        }
    }
}

dependencies {
    implementation(platform(libs.androidx.compose.bom))
    implementation(libs.androidx.activity.compose)
    implementation(libs.androidx.compose.material3)
    implementation(libs.androidx.compose.ui)
    implementation(libs.androidx.compose.ui.graphics)
    implementation(libs.androidx.compose.ui.tooling.preview)
    implementation(libs.androidx.core.ktx)
    implementation(libs.androidx.lifecycle.runtime.ktx)
    implementation(libs.androidx.room.runtime)
    implementation(libs.androidx.room.ktx)
    implementation("com.google.android.gms:play-services-code-scanner:16.1.0")
    ksp(libs.androidx.room.compiler)
    testImplementation(libs.junit)
    androidTestImplementation(platform(libs.androidx.compose.bom))
    androidTestImplementation(libs.androidx.compose.ui.test.junit4)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.junit)
    debugImplementation(libs.androidx.compose.ui.test.manifest)
    debugImplementation(libs.androidx.compose.ui.tooling)
}
