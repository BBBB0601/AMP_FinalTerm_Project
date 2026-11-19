package com.example.myapplication

import android.os.Bundle
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Home
import androidx.compose.material.icons.filled.List
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.outlined.Home
import androidx.compose.material.icons.outlined.List
import androidx.compose.material.icons.outlined.Settings
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.example.myapplication.model.FurnitureConfig
import com.example.myapplication.ui.HomeScreenUI
import com.example.myapplication.ui.SavedLayoutsScreenUI
import com.example.myapplication.ui.SettingsScreenUI
import com.example.myapplication.ui.theme.MyApplicationTheme
import com.example.myapplication.ui.theme.PurpleLightBg
import com.example.myapplication.ui.theme.PurplePrimary

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MyApplicationTheme {
                MainAppScreen(
                    onNavigateToAR = { config ->
                        Toast.makeText(
                            this,
                            "AR 진입: ${config.name} (${config.widthM}m x ${config.depthM}m x ${config.heightM}m)",
                            Toast.LENGTH_LONG
                        ).show()
                    }
                )
            }
        }
    }
}

@Composable
fun MainAppScreen(onNavigateToAR: (FurnitureConfig) -> Unit) {
    var selectedTab by remember { mutableStateOf(0) } // 0: 홈, 1: 저장한 배치, 2: 설정

    Scaffold(
        bottomBar = {
            NavigationBar(
                containerColor = Color.White,
                tonalElevation = 8.dp
            ) {
                NavigationBarItem(
                    selected = selectedTab == 0,
                    onClick = { selectedTab = 0 },
                    icon = {
                        Icon(
                            if (selectedTab == 0) Icons.Filled.Home else Icons.Outlined.Home,
                            contentDescription = "홈"
                        )
                    },
                    label = { Text("홈") },
                    colors = NavigationBarItemDefaults.colors(
                        selectedIconColor = PurplePrimary,
                        selectedTextColor = PurplePrimary,
                        indicatorColor = PurpleLightBg
                    )
                )
                NavigationBarItem(
                    selected = selectedTab == 1,
                    onClick = { selectedTab = 1 },
                    icon = {
                        Icon(
                            if (selectedTab == 1) Icons.Filled.List else Icons.Outlined.List,
                            contentDescription = "저장한 배치"
                        )
                    },
                    label = { Text("저장한 배치") },
                    colors = NavigationBarItemDefaults.colors(
                        selectedIconColor = PurplePrimary,
                        selectedTextColor = PurplePrimary,
                        indicatorColor = PurpleLightBg
                    )
                )
                NavigationBarItem(
                    selected = selectedTab == 2,
                    onClick = { selectedTab = 2 },
                    icon = {
                        Icon(
                            if (selectedTab == 2) Icons.Filled.Settings else Icons.Outlined.Settings,
                            contentDescription = "설정"
                        )
                    },
                    label = { Text("설정") },
                    colors = NavigationBarItemDefaults.colors(
                        selectedIconColor = PurplePrimary,
                        selectedTextColor = PurplePrimary,
                        indicatorColor = PurpleLightBg
                    )
                )
            }
        }
    ) { innerPadding ->
        Box(modifier = Modifier.padding(innerPadding)) {
            when (selectedTab) {
                0 -> HomeScreenUI(onNavigateToAR = onNavigateToAR)
                1 -> SavedLayoutsScreenUI(onNavigateToAR = onNavigateToAR)
                2 -> SettingsScreenUI()
            }
        }
    }
}