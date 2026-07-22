package hu.beosztasvarazslo.app.ui.common

import android.content.Context
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.fragment.app.Fragment
import hu.beosztasvarazslo.app.BeosztasApp
import hu.beosztasvarazslo.app.data.AppRepository

fun Fragment.app(): BeosztasApp = requireActivity().application as BeosztasApp
fun Fragment.repo(): AppRepository = app().repository

fun Fragment.toast(message: String) {
    Toast.makeText(requireContext(), message, Toast.LENGTH_LONG).show()
}

fun Context.confirmDialog(title: String = "Megerősítés", message: String, onYes: () -> Unit) {
    AlertDialog.Builder(this)
        .setTitle(title)
        .setMessage(message)
        .setNegativeButton("Mégse", null)
        .setPositiveButton("Igen") { _, _ -> onYes() }
        .show()
}
