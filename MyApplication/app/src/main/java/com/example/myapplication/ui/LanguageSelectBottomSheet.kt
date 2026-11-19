package com.example.myapplication.ui

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.myapplication.ui.theme.*

@Composable
fun LanguageSelectBottomSheetContent(
    currentLanguage: String,
    onLanguageSelected: (String) -> Unit,
    onClose: () -> Unit
) {
    val languages = listOf(
        "한국어" to "Korean",
        "English" to "English",
        "日本語" to "Japanese",
        "简体中文" to "Chinese (Simplified)"
    )

    Column(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 20.dp)
            .padding(bottom = 32.dp)
    ) {
        Row(
            modifier = Modifier.fillMaxWidth(),
            horizontalArrangement = Arrangement.SpaceBetween,
            verticalAlignment = Alignment.CenterVertically
        ) {
            Column {
                Text("LANGUAGE", fontSize = 11.sp, fontWeight = FontWeight.Bold, color = PurplePrimary)
                Text("언어를 선택하세요", fontSize = 18.sp, fontWeight = FontWeight.Bold, color = TextDark)
            }
            IconButton(onClick = onClose) {
                Icon(Icons.Default.Close, contentDescription = "닫기", tint = TextMuted)
            }
        }

        Spacer(modifier = Modifier.height(16.dp))

        languages.forEach { (langName, langSub) ->
            val isSelected = (currentLanguage == langName)
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(12.dp))
                    .background(if (isSelected) PurpleLightBg else Color.Transparent)
                    .clickable { onLanguageSelected(langName) }
                    .padding(horizontal = 16.dp, vertical = 14.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                Column {
                    Text(
                        langName,
                        fontSize = 15.sp,
                        fontWeight = if (isSelected) FontWeight.Bold else FontWeight.Medium,
                        color = if (isSelected) PurplePrimary else TextDark
                    )
                    Text(langSub, fontSize = 11.sp, color = TextMuted)
                }
                if (isSelected) {
                    Box(
                        modifier = Modifier
                            .size(24.dp)
                            .clip(CircleShape)
                            .background(PurplePrimary),
                        contentAlignment = Alignment.Center
                    ) {
                        Icon(Icons.Default.Check, contentDescription = "선택됨", tint = Color.White, modifier = Modifier.size(16.dp))
                    }
                }
            }
        }
    }
}