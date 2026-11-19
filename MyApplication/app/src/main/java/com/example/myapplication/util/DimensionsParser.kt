package com.example.myapplication.util

object DimensionsParser {
    data class ParsedDimensions(val width: Float, val depth: Float, val height: Float)

    fun parseText(text: String): ParsedDimensions? {
        val cleanText = text.replace(" ", "").lowercase()
        val explicitRegex = Regex("""(?:가로|w|폭)?\s*(\d+(?:\.\d+)?)\s*(?:cm)?\s*[\*x×,]\s*(?:깊이|세로|d)?\s*(\d+(?:\.\d+)?)\s*(?:cm)?\s*[\*x×,]\s*(?:높이|h)?\s*(\d+(?:\.\d+)?)""", RegexOption.IGNORE_CASE)
        val match1 = explicitRegex.find(cleanText)
        if (match1 != null) {
            val (w, d, h) = match1.destructured
            return ParsedDimensions(w.toFloat(), d.toFloat(), h.toFloat())
        }
        val simpleRegex = Regex("""(\d+(?:\.\d+)?)\s*[\*x×]\s*(\d+(?:\.\d+)?)\s*[\*x×]\s*(\d+(?:\.\d+)?)""")
        val match2 = simpleRegex.find(cleanText)
        if (match2 != null) {
            val (w, d, h) = match2.destructured
            return ParsedDimensions(w.toFloat(), d.toFloat(), h.toFloat())
        }
        return null
    }
}