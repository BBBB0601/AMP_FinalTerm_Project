package com.example.myapplication.ui

import android.widget.Toast
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Refresh
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalClipboardManager
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.model.AppDatabase
import com.example.myapplication.model.FurnitureConfig
import com.example.myapplication.model.FurnitureEntity
import com.example.myapplication.model.PRESETS
import com.example.myapplication.ui.theme.*
import com.example.myapplication.util.DimensionsParser
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun HomeScreenUI(onNavigateToAR: (FurnitureConfig) -> Unit) {
    val context = LocalContext.current
    val clipboardManager = LocalClipboardManager.current
    val coroutineScope = rememberCoroutineScope()
    val db = remember { AppDatabase.getDatabase(context) }

    var name by remember { mutableStateOf("책상") }
    var widthText by remember { mutableStateOf("120") }
    var depthText by remember { mutableStateOf("60") }
    var heightText by remember { mutableStateOf("75") }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
            .padding(horizontal = 20.dp)
            .verticalScroll(rememberScrollState())
    ) {
        Spacer(modifier = Modifier.height(48.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(32.dp)
                        .clip(RoundedCornerShape(8.dp))
                        .background(PurplePrimary),
                    contentAlignment = Alignment.Center
                ) {
                    Text("📦", fontSize = 16.sp)
                }
                Spacer(modifier = Modifier.width(8.dp))
                Text("RoomFit AR", fontSize = 18.sp, fontWeight = FontWeight.Bold, color = TextDark)
            }
            IconButton(onClick = {
                name = "책상"
                widthText = "120"
                depthText = "60"
                heightText = "75"
            }) {
                Icon(Icons.Default.Refresh, contentDescription = "초기화", tint = TextMuted)
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        Row {
            Text("내 방에 ", fontSize = 24.sp, fontWeight = FontWeight.Bold, color = TextDark)
            Text("미리 놓아보세요.", fontSize = 24.sp, fontWeight = FontWeight.Bold, color = PurplePrimary)
        }
        Spacer(modifier = Modifier.height(6.dp))
        Text(
            "가구 치수만 입력하면 실제 크기로 배치하고 동선까지 확인할 수 있어요.",
            fontSize = 13.sp,
            color = TextMuted,
            lineHeight = 18.sp
        )

        Spacer(modifier = Modifier.height(20.dp))

        Card(
            shape = RoundedCornerShape(16.dp),
            colors = CardDefaults.cardColors(containerColor = CardGrayBg),
            modifier = Modifier.fillMaxWidth()
        ) {
            Column(modifier = Modifier.padding(16.dp)) {
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Text("STEP 1", fontSize = 11.sp, fontWeight = FontWeight.Bold, color = PurplePrimary)
                    Text("⏱ 약 30초", fontSize = 11.sp, color = TextMuted)
                }
                Spacer(modifier = Modifier.height(4.dp))
                Text("가구 정보를 알려주세요", fontSize = 16.sp, fontWeight = FontWeight.Bold, color = TextDark)

                Spacer(modifier = Modifier.height(16.dp))

                Text("가구 이름", fontSize = 12.sp, color = TextMuted)
                Spacer(modifier = Modifier.height(4.dp))
                OutlinedTextField(
                    value = name,
                    onValueChange = { name = it },
                    modifier = Modifier.fillMaxWidth(),
                    shape = RoundedCornerShape(12.dp),
                    colors = OutlinedTextFieldDefaults.colors(
                        focusedContainerColor = Color.White,
                        unfocusedContainerColor = Color.White,
                        focusedBorderColor = PurplePrimary,
                        unfocusedBorderColor = BorderLight
                    ),
                    singleLine = true
                )

                Spacer(modifier = Modifier.height(12.dp))

                Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text("가로 W", fontSize = 12.sp, color = TextMuted)
                        Spacer(modifier = Modifier.height(4.dp))
                        OutlinedTextField(
                            value = widthText,
                            onValueChange = { widthText = it },
                            suffix = { Text("cm", fontSize = 12.sp, color = TextMuted) },
                            shape = RoundedCornerShape(12.dp),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedContainerColor = Color.White,
                                unfocusedContainerColor = Color.White,
                                focusedBorderColor = PurplePrimary,
                                unfocusedBorderColor = BorderLight
                            ),
                            singleLine = true
                        )
                    }
                    Column(modifier = Modifier.weight(1f)) {
                        Text("깊이 D", fontSize = 12.sp, color = TextMuted)
                        Spacer(modifier = Modifier.height(4.dp))
                        OutlinedTextField(
                            value = depthText,
                            onValueChange = { depthText = it },
                            suffix = { Text("cm", fontSize = 12.sp, color = TextMuted) },
                            shape = RoundedCornerShape(12.dp),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedContainerColor = Color.White,
                                unfocusedContainerColor = Color.White,
                                focusedBorderColor = PurplePrimary,
                                unfocusedBorderColor = BorderLight
                            ),
                            singleLine = true
                        )
                    }
                    Column(modifier = Modifier.weight(1f)) {
                        Text("높이 H", fontSize = 12.sp, color = TextMuted)
                        Spacer(modifier = Modifier.height(4.dp))
                        OutlinedTextField(
                            value = heightText,
                            onValueChange = { heightText = it },
                            suffix = { Text("cm", fontSize = 12.sp, color = TextMuted) },
                            shape = RoundedCornerShape(12.dp),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedContainerColor = Color.White,
                                unfocusedContainerColor = Color.White,
                                focusedBorderColor = PurplePrimary,
                                unfocusedBorderColor = BorderLight
                            ),
                            singleLine = true
                        )
                    }
                }

                Spacer(modifier = Modifier.height(16.dp))

                Surface(
                    onClick = {
                        val clipData = clipboardManager.getText()?.text
                        if (!clipData.isNullOrBlank()) {
                            val parsed = DimensionsParser.parseText(clipData)
                            if (parsed != null) {
                                widthText = parsed.width.toInt().toString()
                                depthText = parsed.depth.toInt().toString()
                                heightText = parsed.height.toInt().toString()
                                Toast.makeText(context, "치수를 자동으로 추출했습니다!", Toast.LENGTH_SHORT).show()
                            } else {
                                Toast.makeText(context, "치수 형식을 찾을 수 없습니다.", Toast.LENGTH_SHORT).show()
                            }
                        } else {
                            Toast.makeText(context, "클립보드가 비어있습니다.", Toast.LENGTH_SHORT).show()
                        }
                    },
                    shape = RoundedCornerShape(12.dp),
                    color = Color.White,
                    border = androidx.compose.foundation.BorderStroke(1.dp, BorderLight),
                    modifier = Modifier.fillMaxWidth()
                ) {
                    Row(
                        modifier = Modifier.padding(14.dp),
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Text("📋", fontSize = 16.sp)
                        Spacer(modifier = Modifier.width(10.dp))
                        Column(modifier = Modifier.weight(1f)) {
                            Text("거래 글에서 치수 가져오기", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = TextDark)
                            Text("복사한 글에서 숫자를 자동으로 찾아드려요", fontSize = 11.sp, color = TextMuted)
                        }
                        Text(">", fontSize = 14.sp, color = TextMuted)
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(20.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("자주 찾는 가구", fontSize = 15.sp, fontWeight = FontWeight.Bold, color = TextDark)
            Text("탭하면 바로 입력돼요", fontSize = 11.sp, color = TextMuted)
        }
        Spacer(modifier = Modifier.height(10.dp))

        Row(horizontalArrangement = Arrangement.spacedBy(8.dp), modifier = Modifier.fillMaxWidth()) {
            PRESETS.forEach { preset ->
                val isSelected = (name == preset.name)
                Card(
                    modifier = Modifier
                        .weight(1f)
                        .clickable {
                            name = preset.name
                            widthText = preset.w.toInt().toString()
                            depthText = preset.d.toInt().toString()
                            heightText = preset.h.toInt().toString()
                        },
                    shape = RoundedCornerShape(12.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = if (isSelected) PurpleLightBg else CardGrayBg
                    ),
                    border = if (isSelected) androidx.compose.foundation.BorderStroke(1.5.dp, PurplePrimary) else null
                ) {
                    Column(
                        modifier = Modifier
                            .padding(12.dp)
                            .fillMaxWidth(),
                        horizontalAlignment = Alignment.CenterHorizontally
                    ) {
                        Box(
                            modifier = Modifier
                                .size(32.dp)
                                .clip(CircleShape)
                                .background(if (isSelected) PurplePrimary else Color.White),
                            contentAlignment = Alignment.Center
                        ) {
                            Text("📦", fontSize = 14.sp)
                        }
                        Spacer(modifier = Modifier.height(6.dp))
                        Text(preset.name, fontSize = 12.sp, fontWeight = FontWeight.Bold, color = TextDark)
                        Text(
                            "${preset.w.toInt()} × ${preset.d.toInt()} × ${preset.h.toInt()}",
                            fontSize = 10.sp,
                            color = TextMuted
                        )
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))

        Button(
            onClick = {
                val w = widthText.toFloatOrNull() ?: 0f
                val d = depthText.toFloatOrNull() ?: 0f
                val h = heightText.toFloatOrNull() ?: 0f

                if (w in 10f..300f && d in 10f..300f && h in 10f..300f) {
                    val config = FurnitureConfig(name = name, widthCm = w, depthCm = d, heightCm = h)

                    // Room DB에 동기화 저장
                    coroutineScope.launch {
                        db.furnitureDao().insertFurniture(
                            FurnitureEntity(
                                name = name,
                                widthCm = w,
                                depthCm = d,
                                heightCm = h,
                                xOffsetDp = (20..150).random().toFloat(),
                                yOffsetDp = (20..200).random().toFloat(),
                                isNew = true
                            )
                        )
                    }

                    onNavigateToAR(config)
                } else {
                    Toast.makeText(context, "치수는 10cm ~ 300cm 범위 내로 입력해주세요.", Toast.LENGTH_SHORT).show()
                }
            },
            modifier = Modifier
                .fillMaxWidth()
                .height(54.dp),
            shape = RoundedCornerShape(14.dp),
            colors = ButtonDefaults.buttonColors(containerColor = PurplePrimary)
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text("📷 ", fontSize = 16.sp)
                Text("AR로 공간 확인하기", fontSize = 16.sp, fontWeight = FontWeight.Bold)
            }
        }

        Spacer(modifier = Modifier.height(8.dp))

        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.Center,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Text("🛡 입력한 치수는 기기에만 안전하게 저장돼요", fontSize = 11.sp, color = TextMuted)
        }

        Spacer(modifier = Modifier.height(24.dp))
    }
}