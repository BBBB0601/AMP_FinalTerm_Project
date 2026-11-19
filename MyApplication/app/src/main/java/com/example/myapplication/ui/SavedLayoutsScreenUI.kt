package com.example.myapplication.ui

import android.widget.Toast
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Share
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.model.AppDatabase
import com.example.myapplication.model.FurnitureConfig
import com.example.myapplication.ui.theme.*
import com.example.myapplication.util.ShareUtils
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun SavedLayoutsScreenUI(onNavigateToAR: (FurnitureConfig) -> Unit) {
    val context = LocalContext.current
    val view = LocalView.current
    val coroutineScope = rememberCoroutineScope()
    val db = remember { AppDatabase.getDatabase(context) }

    val placedFurnitureList by db.furnitureDao().getAllFurniture().collectAsState(initial = emptyList())

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
            Column {
                Text("MY ROOM LAYOUT", fontSize = 11.sp, fontWeight = FontWeight.Bold, color = PurplePrimary)
                Text("저장된 원룸 도면", fontSize = 22.sp, fontWeight = FontWeight.Bold, color = TextDark)
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Surface(
                    shape = RoundedCornerShape(20.dp),
                    color = PurpleLightBg
                ) {
                    Text(
                        "총 ${placedFurnitureList.size}개 가구",
                        fontSize = 12.sp,
                        fontWeight = FontWeight.Bold,
                        color = PurplePrimary,
                        modifier = Modifier.padding(horizontal = 12.dp, vertical = 6.dp)
                    )
                }

                IconButton(onClick = {
                    if (placedFurnitureList.isNotEmpty()) {
                        val bitmap = ShareUtils.captureView(view)
                        ShareUtils.shareBitmap(context, bitmap)
                    } else {
                        Toast.makeText(context, "공유할 가구 배치 정보가 없습니다.", Toast.LENGTH_SHORT).show()
                    }
                }) {
                    Icon(Icons.Default.Share, contentDescription = "공유하기", tint = PurplePrimary)
                }

                if (placedFurnitureList.isNotEmpty()) {
                    IconButton(onClick = {
                        coroutineScope.launch {
                            db.furnitureDao().deleteAll()
                            Toast.makeText(context, "모든 가구 배치가 초기화되었습니다.", Toast.LENGTH_SHORT).show()
                        }
                    }) {
                        Icon(Icons.Default.Delete, contentDescription = "전체 삭제", tint = TextMuted)
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(6.dp))
        Text(
            "배치된 가구 위치와 새로 검증한 가구를 평면도에서 확인하세요.",
            fontSize = 13.sp,
            color = TextMuted
        )

        Spacer(modifier = Modifier.height(16.dp))

        // ui/ReportCard.kt의 컴포넌트 불러오기
        if (placedFurnitureList.isNotEmpty()) {
            FurnitureReportCard(furnitureList = placedFurnitureList)
        }

        Row(
            modifier = Modifier
                .fillMaxWidth()
                .background(CardGrayBg, shape = RoundedCornerShape(12.dp))
                .padding(horizontal = 16.dp, vertical = 10.dp),
            horizontalArrangement = Arrangement.SpaceEvenly,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(12.dp)
                        .clip(RoundedCornerShape(3.dp))
                        .background(PurplePrimary)
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text("새로 추가된 가구", fontSize = 12.sp, fontWeight = FontWeight.Medium, color = TextDark)
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Box(
                    modifier = Modifier
                        .size(12.dp)
                        .clip(RoundedCornerShape(3.dp))
                        .background(Color(0xFF4A4A4A))
                )
                Spacer(modifier = Modifier.width(6.dp))
                Text("기존 배치 가구", fontSize = 12.sp, fontWeight = FontWeight.Medium, color = TextDark)
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        Card(
            shape = RoundedCornerShape(20.dp),
            colors = CardDefaults.cardColors(containerColor = Color(0xFFF0F2F5)),
            modifier = Modifier
                .fillMaxWidth()
                .height(380.dp),
            border = androidx.compose.foundation.BorderStroke(2.dp, BorderLight)
        ) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp)
            ) {
                Box(
                    modifier = Modifier
                        .fillMaxSize()
                        .border(3.dp, Color(0xFF2C3E50), RoundedCornerShape(8.dp))
                        .background(Color.White)
                ) {
                    Box(
                        modifier = Modifier
                            .align(Alignment.BottomStart)
                            .padding(start = 20.dp)
                            .size(40.dp, 6.dp)
                            .background(Color(0xFFE74C3C))
                    )
                    Text(
                        "현관",
                        fontSize = 9.sp,
                        color = Color(0xFFE74C3C),
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier
                            .align(Alignment.BottomStart)
                            .padding(start = 26.dp, bottom = 8.dp)
                    )

                    Box(
                        modifier = Modifier
                            .align(Alignment.TopCenter)
                            .size(80.dp, 6.dp)
                            .background(Color(0xFF3498DB))
                    )

                    if (placedFurnitureList.isEmpty()) {
                        Box(
                            modifier = Modifier.fillMaxSize(),
                            contentAlignment = Alignment.Center
                        ) {
                            Text("저장된 가구가 없습니다.\n홈 탭에서 가구를 추가해보세요!", fontSize = 13.sp, color = TextMuted)
                        }
                    } else {
                        placedFurnitureList.forEach { furniture ->
                            val itemBgColor = if (furniture.isNew) PurplePrimary else Color(0xFF4A4A4A)
                            val itemBorderColor = if (furniture.isNew) PurpleLightBg else Color.Transparent

                            Box(
                                modifier = Modifier
                                    .offset(x = furniture.xOffsetDp.dp, y = furniture.yOffsetDp.dp)
                                    .size(
                                        width = (furniture.widthCm * 0.6f).dp.coerceIn(40.dp, 160.dp),
                                        height = (furniture.depthCm * 0.6f).dp.coerceIn(30.dp, 120.dp)
                                    )
                                    .clip(RoundedCornerShape(6.dp))
                                    .background(itemBgColor)
                                    .border(1.5.dp, itemBorderColor, RoundedCornerShape(6.dp))
                                    .clickable {
                                        Toast.makeText(
                                            context,
                                            "${furniture.name} (${furniture.widthCm.toInt()}x${furniture.depthCm.toInt()}cm)",
                                            Toast.LENGTH_SHORT
                                        ).show()
                                    },
                                contentAlignment = Alignment.Center
                            ) {
                                Text(
                                    text = furniture.name,
                                    fontSize = 10.sp,
                                    fontWeight = FontWeight.Bold,
                                    color = Color.White,
                                    maxLines = 1
                                )
                            }
                        }
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(20.dp))

        Text("배치된 가구 목록", fontSize = 15.sp, fontWeight = FontWeight.Bold, color = TextDark)
        Spacer(modifier = Modifier.height(10.dp))

        if (placedFurnitureList.isEmpty()) {
            Text("배치된 가구가 없습니다.", fontSize = 13.sp, color = TextMuted)
        } else {
            placedFurnitureList.forEach { furniture ->
                Card(
                    shape = RoundedCornerShape(12.dp),
                    colors = CardDefaults.cardColors(
                        containerColor = if (furniture.isNew) PurpleLightBg else CardGrayBg
                    ),
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(bottom = 8.dp)
                ) {
                    Row(
                        modifier = Modifier
                            .fillMaxWidth()
                            .padding(14.dp),
                        horizontalArrangement = Arrangement.SpaceBetween,
                        verticalAlignment = Alignment.CenterVertically
                    ) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Box(
                                modifier = Modifier
                                    .size(10.dp)
                                    .clip(CircleShape)
                                    .background(if (furniture.isNew) PurplePrimary else Color(0xFF4A4A4A))
                            )
                            Spacer(modifier = Modifier.width(10.dp))
                            Column {
                                Row(verticalAlignment = Alignment.CenterVertically) {
                                    Text(
                                        furniture.name,
                                        fontSize = 14.sp,
                                        fontWeight = FontWeight.Bold,
                                        color = TextDark
                                    )
                                    if (furniture.isNew) {
                                        Spacer(modifier = Modifier.width(6.dp))
                                        Surface(
                                            shape = RoundedCornerShape(4.dp),
                                            color = PurplePrimary
                                        ) {
                                            Text(
                                                "NEW",
                                                fontSize = 9.sp,
                                                fontWeight = FontWeight.Bold,
                                                color = Color.White,
                                                modifier = Modifier.padding(horizontal = 4.dp, vertical = 1.dp)
                                            )
                                        }
                                    }
                                }
                                Text(
                                    "가로 ${furniture.widthCm.toInt()}cm × 깊이 ${furniture.depthCm.toInt()}cm",
                                    fontSize = 12.sp,
                                    color = TextMuted
                                )
                            }
                        }

                        Row(verticalAlignment = Alignment.CenterVertically) {
                            OutlinedButton(
                                onClick = {
                                    val config = FurnitureConfig(
                                        name = furniture.name,
                                        widthCm = furniture.widthCm,
                                        depthCm = furniture.depthCm,
                                        heightCm = furniture.heightCm
                                    )
                                    onNavigateToAR(config)
                                },
                                shape = RoundedCornerShape(8.dp),
                                contentPadding = PaddingValues(horizontal = 10.dp, vertical = 4.dp)
                            ) {
                                Text("AR 재검증", fontSize = 11.sp, color = PurplePrimary)
                            }

                            IconButton(onClick = {
                                coroutineScope.launch {
                                    db.furnitureDao().deleteFurniture(furniture)
                                }
                            }) {
                                Icon(
                                    Icons.Default.Delete,
                                    contentDescription = "삭제",
                                    tint = TextMuted,
                                    modifier = Modifier.size(18.dp)
                                )
                            }
                        }
                    }
                }
            }
        }

        Spacer(modifier = Modifier.height(24.dp))
    }
}