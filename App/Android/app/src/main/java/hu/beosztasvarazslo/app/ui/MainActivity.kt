package hu.beosztasvarazslo.app.ui

import android.net.Uri
import android.os.Bundle
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.fragment.app.Fragment
import androidx.lifecycle.lifecycleScope
import androidx.viewpager2.adapter.FragmentStateAdapter
import com.google.android.material.tabs.TabLayoutMediator
import hu.beosztasvarazslo.app.BeosztasApp
import hu.beosztasvarazslo.app.R
import hu.beosztasvarazslo.app.databinding.ActivityMainBinding
import hu.beosztasvarazslo.app.ui.common.confirmDialog
import hu.beosztasvarazslo.app.ui.employees.EmployeesFragment
import hu.beosztasvarazslo.app.ui.groups.GroupsFragment
import hu.beosztasvarazslo.app.ui.print.PrintFragment
import hu.beosztasvarazslo.app.ui.schedule.ScheduleFragment
import hu.beosztasvarazslo.app.ui.settings.SettingsFragment
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private val tabTitles = listOf("Beállítások", "Csoportok", "Dolgozók", "Beosztás", "Nyomtatás")

    private val exportLauncher = registerForActivityResult(ActivityResultContracts.CreateDocument("application/json")) { uri ->
        if (uri != null) doExport(uri)
    }
    private val importLauncher = registerForActivityResult(ActivityResultContracts.OpenDocument()) { uri ->
        if (uri != null) doImport(uri)
    }

    private val app: BeosztasApp by lazy { application as BeosztasApp }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)
        setSupportActionBar(binding.toolbar)

        binding.viewPager.adapter = object : FragmentStateAdapter(this) {
            override fun getItemCount() = 5
            override fun createFragment(position: Int): Fragment = when (position) {
                0 -> SettingsFragment()
                1 -> GroupsFragment()
                2 -> EmployeesFragment()
                3 -> ScheduleFragment()
                else -> PrintFragment()
            }
        }
        binding.viewPager.offscreenPageLimit = 4
        TabLayoutMediator(binding.tabLayout, binding.viewPager) { tab, position ->
            tab.text = tabTitles[position]
        }.attach()

        lifecycleScope.launch { app.repository.ensureSeeded() }
    }

    override fun onCreateOptionsMenu(menu: android.view.Menu): Boolean {
        menuInflater.inflate(R.menu.menu_main, menu)
        return true
    }

    override fun onOptionsItemSelected(item: android.view.MenuItem): Boolean {
        when (item.itemId) {
            R.id.action_export_db -> {
                val stamp = SimpleDateFormat("yyyy-MM-dd_HHmm", Locale.getDefault()).format(Date())
                exportLauncher.launch("beosztas-adatbazis-$stamp.json")
                return true
            }
            R.id.action_import_db -> {
                confirmDialog(message = "A betöltés felülírja a jelenlegi adatokat. Folytatod?") {
                    importLauncher.launch(arrayOf("application/json", "text/*", "*/*"))
                }
                return true
            }
            R.id.action_reset_db -> {
                confirmDialog(message = "Ez törli az ÖSSZES adatot (munkacsoportok, dolgozók, beosztások) és visszaáll az alapértelmezett állapotra. Biztosan folytatod?") {
                    lifecycleScope.launch {
                        app.repository.resetAll()
                        Toast.makeText(this@MainActivity, "Alaphelyzet visszaállítva.", Toast.LENGTH_LONG).show()
                        recreate()
                    }
                }
                return true
            }
        }
        return super.onOptionsItemSelected(item)
    }

    private fun doExport(uri: Uri) {
        lifecycleScope.launch {
            try {
                val json = app.repository.exportToJson()
                contentResolver.openOutputStream(uri)?.use { it.write(json.toByteArray(Charsets.UTF_8)) }
                Toast.makeText(this@MainActivity, "Adatbázis fájlba mentve.", Toast.LENGTH_LONG).show()
            } catch (e: Exception) {
                Toast.makeText(this@MainActivity, "Hiba a mentéskor: ${e.message}", Toast.LENGTH_LONG).show()
            }
        }
    }

    private fun doImport(uri: Uri) {
        lifecycleScope.launch {
            try {
                val text = contentResolver.openInputStream(uri)?.bufferedReader(Charsets.UTF_8)?.use { it.readText() }
                if (text == null) {
                    Toast.makeText(this@MainActivity, "Nem sikerült beolvasni a fájlt.", Toast.LENGTH_LONG).show()
                    return@launch
                }
                val error = app.repository.importFromJson(text)
                if (error != null) {
                    Toast.makeText(this@MainActivity, error, Toast.LENGTH_LONG).show()
                } else {
                    Toast.makeText(this@MainActivity, "Adatbázis betöltve.", Toast.LENGTH_LONG).show()
                    recreate()
                }
            } catch (e: Exception) {
                Toast.makeText(this@MainActivity, "Hiba a betöltéskor: ${e.message}", Toast.LENGTH_LONG).show()
            }
        }
    }
}
