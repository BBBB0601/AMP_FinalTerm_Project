package com.example.myapplication.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.ui.theme.*

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SettingsScreenUI() {
    var selectedLanguage by remember { mutableStateOf("한국어") }
    var selectedUnit by remember { mutableStateOf("cm") }
    var isVibrationEnabled by remember { mutableStateOf(true) }
    var isHighQualityAREnabled by remember { mutableStateOf(false) }

    var showLanguageBottomSheet by remember { mutableStateOf(false) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
            .padding(horizontal = 20.dp)
            .verticalScroll(rememberScrollState())
    ) {
        Spacer(modifier = Modifier.height(48.dp))

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = PurplePrimary),
            modifier = Modifier.fillMaxWidth()
        ) {
            Row(
                modifier = Modifier.padding(20.dp),
                verticalAlignment = Alignment.CenterVertically
            ) {
                Box(
                    modifier = Modifier
                        .size(40.dp)
                        .clip(RoundedCornerShape(10.dp))
                        .background(Color.White.copy(alpha = 0.2f)),
                    contentAlignment = Alignment.Center
                ) {
                    Text("📦", fontSize = 20.sp)
                }
                Spacer(modifier = Modifier.width(14.dp))
                Column {
                    Text("RoomFit AR", fontSize = 18.sp, fontWeight = FontWeight.Bold, color = Color.White)
                    Text("나에게 편한 방식으로 설정하세요", fontSize = 12.sp, color = Color.White.copy(alpha = 0.8f))
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        Text("일반", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = TextMuted)
        Spacer(modifier = Modifier.height(8.dp))

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = CardGrayBg),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clickable { showLanguageBottomSheet = true }
                        .padding(vertical = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("🌐", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Text("언어", fontSize = 15.sp, fontWeight = FontWeight.Medium, color = TextDark)
                    }
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text(selectedLanguage, fontSize = 13.sp, color = TextMuted)
                        Spacer(modifier = Modifier.width(4.dp))
                        Text(">", fontSize = 14.sp, color = TextMuted)
                    }
                }

                HorizontalDivider(color = BorderLight)

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("✏️", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Text("측정 단위", fontSize = 15.sp, fontWeight = FontWeight.Medium, color = TextDark)
                    }
                    Surface(
                        shape = RoundedCornerShape(20.dp),
                        color = Color.White,
                        border = androidx.compose.foundation.BorderStroke(1.dp, BorderLight)
                    ) {
                        Row(modifier = Modifier.padding(2.dp)) {
                            Box(
                                modifier = Modifier
                                    .clip(RoundedCornerShape(18.dp))
                                    .background(if (selectedUnit == "cm") PurplePrimary else Color.Transparent)
                                    .clickable { selectedUnit = "cm" }
                                    .padding(horizontal = 12.dp, vertical = 4.dp)
                            ) {
                                Text(
                                    "cm",
                                    fontSize = 12.sp,
                                    fontWeight = FontWeight.Bold,
                                    color = if (selectedUnit == "cm") Color.White else TextMuted
                                )
                            }
                            Box(
                                modifier = Modifier
                                    .clip(RoundedCornerShape(18.dp))
                                    .background(if (selectedUnit == "inch") PurplePrimary else Color.Transparent)
                                    .clickable { selectedUnit = "inch" }
                                    .padding(horizontal = 12.dp, vertical = 4.dp)
                            ) {
                                Text(
                                    "inch",
                                    fontSize = 12.sp,
                                    fontWeight = FontWeight.Bold,
                                    color = if (selectedUnit == "inch") Color.White else TextMuted
                                )
                            }
                        }
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        Text("AR 경험", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = TextMuted)
        Spacer(modifier = Modifier.height(8.dp))

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = CardGrayBg),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("📳", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Column {
                            Text("진동 피드백", fontSize = 15.sp, fontWeight = FontWeight.Medium, color = TextDark)
                            Text("충돌 감지 및 배치 완료 시 진동", fontSize = 11.sp, color = TextMuted)
                        }
                    }
                    Switch(
                        checked = isVibrationEnabled,
                        onCheckedChange = { isVibrationEnabled = it },
                        colors = SwitchDefaults.colors(
                            checkedThumbColor = Color.White,
                            checkedTrackColor = PurplePrimary
                        )
                    )
                }

                HorizontalDivider(color = BorderLight)

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("☁️", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Column {
                            Text("고화질 AR", fontSize = 15.sp, fontWeight = FontWeight.Medium, color = TextDark)
                            Text("안정적인 성능 · 배터리 절약", fontSize = 11.sp, color = TextMuted)
                        }
                    }
                    Switch(
                        checked = isHighQualityAREnabled,
                        onCheckedChange = { isHighQualityAREnabled = it },
                        colors = SwitchDefaults.colors(
                            checkedThumbColor = Color.White,
                            checkedTrackColor = PurplePrimary
                        )
                    )
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        Text("개인정보 및 앱 정보", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = TextMuted)
        Spacer(modifier = Modifier.height(8.dp))

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = CardGrayBg),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.padding(horizontal = 16.dp, vertical = 8.dp)) {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("🛡", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Column {
                            Text("로컬 데이터 보호", fontSize = 15.sp, fontWeight = FontWeight.Medium, color = TextDark)
                            Text("모든 배치 정보는 이 기기에만 저장돼요", fontSize = 11.sp, color = TextMuted)
                        }
                    }
                    Surface(
                        shape = RoundedCornerShape(6.dp),
                        color = Color(0xFFE8F5E9)
                    ) {
                        Text(
                            "안전",
                            fontSize = 11.sp,
                            fontWeight = FontWeight.Bold,
                            color = Color(0xFF2E7D32),
                            modifier = Modifier.padding(horizontal = 8.dp, vertical = 4.dp)
                        )
                    }
                }

                HorizontalDivider(color = BorderLight)

                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(vertical = 12.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        Text("ℹ️", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(12.dp))
                        Text("앱 정보", fontSize = 15.sp, fontWeight = FontWeight.Medium, color = TextDark)
                    }
                    Text("버전 1.0.0", fontSize = 13.sp, color = TextMuted)
                }
            }
        }

        Spacer(modifier = Modifier.height(16.dp))
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("RoomFit AR은 카메라 영상을 서버에 전송하거나 저장하지 않습니다.", fontSize = 10.sp, color = TextMuted)
        }
        Spacer(modifier = Modifier.height(24.dp))
    }

    if (showLanguageBottomSheet) {
        ModalBottomSheet(
            onDismissRequest = { showLanguageBottomSheet = false },
            sheetState = rememberModalBottomSheetState(skipPartiallyExpanded = true),
            containerColor = Color.White
        ) {
            LanguageSelectBottomSheetContent(
                currentLanguage = selectedLanguage,
                onLanguageSelected = { lang ->
                    selectedLanguage = lang
                    showLanguageBottomSheet = false
                },
                onClose = { showLanguageBottomSheet = false }
            )
        }
    }
}