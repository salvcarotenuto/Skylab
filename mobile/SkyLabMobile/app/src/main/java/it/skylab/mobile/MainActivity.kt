package it.skylab.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.BackHandler
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.heightIn
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.WindowInsets
import androidx.compose.foundation.layout.ime
import androidx.compose.foundation.clickable
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.foundation.relocation.BringIntoViewRequester
import androidx.compose.foundation.relocation.bringIntoViewRequester
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DatePicker
import androidx.compose.material3.DatePickerDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.material3.TimePicker
import androidx.compose.material3.rememberDatePickerState
import androidx.compose.material3.rememberTimePickerState
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.rememberCoroutineScope
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.DpOffset
import androidx.compose.ui.unit.sp
import it.skylab.mobile.ui.theme.SkyLabMobileTheme
import com.google.mlkit.vision.barcode.common.Barcode
import com.google.mlkit.vision.codescanner.GmsBarcodeScannerOptions
import com.google.mlkit.vision.codescanner.GmsBarcodeScanning
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.delay
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.text.NumberFormat
import java.time.LocalDate
import java.time.LocalDateTime
import java.time.Instant
import java.time.DayOfWeek
import java.time.ZoneId
import java.time.ZoneOffset
import java.time.format.DateTimeFormatter
import java.time.temporal.TemporalAdjusters
import java.util.Locale

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            SkyLabMobileTheme {
                Scaffold(modifier = Modifier.fillMaxSize()) { innerPadding ->
                    LoginScreen(modifier = Modifier.padding(innerPadding))
                }
            }
        }
    }
}

@Composable
fun LoginScreen(
    modifier: Modifier = Modifier,
    loadUsers: suspend () -> List<String> = ::loadLoginUsers
) {
    val context = LocalContext.current
    val sessionPreferences = remember(context) { context.getSharedPreferences("skylab-session", android.content.Context.MODE_PRIVATE) }
    var username by remember { mutableStateOf(sessionPreferences.getString("username", "").orEmpty()) }
    var password by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }
    var menuExpanded by remember { mutableStateOf(false) }
    var users by remember { mutableStateOf<List<String>>(emptyList()) }
    var loadingUsers by remember { mutableStateOf(true) }
    var usersError by remember { mutableStateOf(false) }
    var loginInProgress by remember { mutableStateOf(false) }
    var loginError by remember { mutableStateOf(false) }
    var passwordFocused by remember { mutableStateOf(false) }
    var loggedIn by remember { mutableStateOf(username.isNotBlank()) }
    var sessionToken by remember { mutableStateOf(sessionPreferences.getString("token", "").orEmpty()) }
    val scope = rememberCoroutineScope()
    val loginButtonRequester = remember { BringIntoViewRequester() }
    val density = LocalDensity.current
    val keyboardVisible = WindowInsets.ime.getBottom(density) > 0
    val compactLogin = passwordFocused || keyboardVisible

    LaunchedEffect(Unit) {
        if (loggedIn) {
            loadingUsers = false
            return@LaunchedEffect
        }
        try {
            users = loadUsers()
        } catch (exception: Exception) {
            usersError = true
            android.util.Log.e("SkyLabMobile", "Caricamento utenti non riuscito", exception)
        } finally {
            loadingUsers = false
        }
    }

    if (loggedIn) {
        WelcomeScreen(
            username = username,
            token = sessionToken,
            onLogout = {
                sessionPreferences.edit().clear().apply()
                username = ""
                sessionToken = ""
                password = ""
                users = emptyList()
                loadingUsers = true
                loggedIn = false
                usersError = false
                scope.launch {
                    try {
                        users = loadUsers()
                    } catch (exception: Exception) {
                        usersError = true
                        android.util.Log.e("SkyLabMobile", "Caricamento utenti non riuscito", exception)
                    } finally {
                        loadingUsers = false
                    }
                }
            },
            modifier = modifier
        )
        return
    }

    Column(
        modifier = modifier.fillMaxSize().imePadding().verticalScroll(rememberScrollState())
            .padding(horizontal = 32.dp, vertical = if (compactLogin) 8.dp else 0.dp),
        verticalArrangement = if (compactLogin) Arrangement.Top else Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "SkyLab Mobile",
            style = MaterialTheme.typography.headlineLarge,
            fontSize = if (compactLogin) 22.sp else 30.sp,
            fontWeight = FontWeight.Bold
        )
        if (!compactLogin) {
            Text(
                text = "Accedi al tuo account",
                style = MaterialTheme.typography.bodyLarge,
                fontSize = 17.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        Spacer(modifier = Modifier.height(if (compactLogin) 8.dp else 32.dp))
        Box(modifier = Modifier.fillMaxWidth()) {
            OutlinedButton(
                onClick = { menuExpanded = true },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                enabled = !loadingUsers && users.isNotEmpty()
            ) {
                if (loadingUsers) {
                    CircularProgressIndicator(modifier = Modifier.height(20.dp))
                } else {
                    Text(username.ifEmpty { "Login utente" }, fontSize = 20.sp)
                }
            }
            DropdownMenu(
                expanded = menuExpanded,
                onDismissRequest = { menuExpanded = false },
                modifier = Modifier.width(280.dp),
                offset = DpOffset(x = 8.dp, y = 0.dp)
            ) {
                users.forEach { user ->
                    DropdownMenuItem(
                        text = { Text(user, fontSize = 20.sp) },
                        onClick = {
                            username = user
                            menuExpanded = false
                        }
                    )
                }
            }
        }
        if (usersError) {
            Text(
                text = "Impossibile caricare gli utenti",
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodySmall
            )
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedTextField(
            value = password,
            onValueChange = { password = it },
            modifier = Modifier.fillMaxWidth().onFocusChanged { state ->
                passwordFocused = state.isFocused
                if (state.isFocused) scope.launch {
                    delay(350)
                    loginButtonRequester.bringIntoView()
                }
            },
            label = { Text("Password", fontSize = 15.sp) },
            textStyle = MaterialTheme.typography.bodyLarge.copy(fontSize = 18.sp),
            singleLine = true,
            visualTransformation = if (passwordVisible) VisualTransformation.None
            else PasswordVisualTransformation(),
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
            trailingIcon = {
                IconButton(onClick = { passwordVisible = !passwordVisible }) {
                    Icon(
                        painter = painterResource(R.drawable.ic_visibility),
                        contentDescription = if (passwordVisible) "Nascondi password" else "Mostra password"
                    )
                }
            }
        )
        Spacer(modifier = Modifier.height(if (compactLogin) 12.dp else 24.dp))
        Button(
            onClick = {
                loginError = false
                loginInProgress = true
                scope.launch {
                    try {
                        sessionToken = authenticate(username, password).orEmpty()
                        loggedIn = sessionToken.isNotEmpty()
                        if (loggedIn) sessionPreferences.edit().putString("username", username).putString("token", sessionToken).apply()
                        loginError = !loggedIn
                    } catch (_: Exception) {
                        loginError = true
                    } finally {
                        loginInProgress = false
                    }
                }
            },
            modifier = Modifier.fillMaxWidth().bringIntoViewRequester(loginButtonRequester),
            enabled = username.isNotBlank() && password.isNotBlank() && !loginInProgress
        ) {
            Text(if (loginInProgress) "Accesso…" else "Accedi", fontSize = 18.sp)
        }
        if (loginError) {
            Text(
                text = "Utente o password non corretti",
                color = MaterialTheme.colorScheme.error,
                style = MaterialTheme.typography.bodyMedium,
                modifier = Modifier.padding(top = 12.dp)
            )
        }
    }
}

@Composable
private fun WelcomeScreen(username: String, token: String, onLogout: () -> Unit, modifier: Modifier = Modifier) {
    var showWorks by remember { mutableStateOf(false) }
    var showAgenda by remember { mutableStateOf(false) }
    if (showWorks) {
        MyWorksScreen(username = username, token = token, onBack = { showWorks = false }, modifier = modifier)
        return
    }
    if (showAgenda) {
        MobileAgendaScreen(username = username, token = token, onBack = { showAgenda = false }, modifier = modifier)
        return
    }
    Column(
        modifier = modifier.fillMaxSize().padding(horizontal = 32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text("SkyLab Mobile", style = MaterialTheme.typography.headlineLarge, fontWeight = FontWeight.Bold)
        Spacer(modifier = Modifier.height(20.dp))
        Text("Benvenuto, $username", fontSize = 22.sp, fontWeight = FontWeight.SemiBold)
        Spacer(modifier = Modifier.height(8.dp))
        Text("Accesso eseguito correttamente", fontSize = 17.sp)
        Spacer(modifier = Modifier.height(36.dp))
        Button(
            onClick = { showWorks = true },
            modifier = Modifier.fillMaxWidth().height(58.dp)
        ) {
            Text("Lavori assegnati", fontSize = 20.sp)
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedButton(
            onClick = { showAgenda = true },
            modifier = Modifier.fillMaxWidth().height(58.dp)
        ) {
            Text("Agenda", fontSize = 20.sp)
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedButton(onClick = onLogout, modifier = Modifier.fillMaxWidth().height(52.dp)) {
            Text("Cambia operatore", fontSize = 18.sp)
        }
    }
}

@Composable
private fun MyWorksScreen(username: String, token: String, onBack: () -> Unit, modifier: Modifier = Modifier) {
    var works by remember { mutableStateOf<List<MobileWork>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var failed by remember { mutableStateOf(false) }
    var offline by remember { mutableStateOf(false) }
    var showAll by remember { mutableStateOf(false) }
    var lastSync by remember { mutableStateOf("") }
    var selectedWorkId by remember { mutableStateOf<Int?>(null) }
    var syncNotice by remember { mutableStateOf<String?>(null) }
    val worksListState = rememberLazyListState()
    val context = LocalContext.current
    val workDao = remember(context) { SkyLabDatabase.get(context).cachedWorks() }
    val catalogDao = remember(context) { SkyLabDatabase.get(context).mobileCatalog() }
    val reportDao = remember(context) { SkyLabDatabase.get(context).workReportDrafts() }
    val scope = rememberCoroutineScope()

    selectedWorkId?.let { workId ->
        MobileWorkDetailScreen(username = username, token = token, workId = workId, workDao = workDao, onBack = { selectedWorkId = null }, modifier = modifier)
        return
    }

    val synchronize: () -> Unit = {
        scope.launch {
            syncNotice = null
            loading = true
            failed = false
            offline = false
            try {
                val previousIds = works.mapTo(mutableSetOf()) { it.id }
                val synchronizedWorks = synchronizeMyWorks(token, username, workDao)
                val catalogCount = try { synchronizeMobileCatalog(token, catalogDao) } catch (_: Exception) { null }
                val sentReports = retryPendingReports(token, username, reportDao)
                val newCount = synchronizedWorks.count { it.id !in previousIds }
                works = synchronizedWorks
                lastSync = LocalDateTime.now().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"))
                val message = synchronizationNotice(newCount, catalogCount, sentReports)
                syncNotice = message
                launch {
                    delay(10_000)
                    if (syncNotice == message) syncNotice = null
                }
            } catch (_: Exception) {
                if (works.isEmpty()) failed = true else offline = true
                syncNotice = connectionFailureNotice(works.isNotEmpty())
                launch {
                    delay(5_000)
                    syncNotice = null
                }
            } finally {
                loading = false
            }
        }
        Unit
    }

    LaunchedEffect(token) {
        val cachedWorks = loadCachedWorks(username, workDao)
        if (cachedWorks.isNotEmpty()) {
            works = cachedWorks
            val cachedAt = withContext(Dispatchers.IO) { workDao.lastSynchronization(username) }
            if (cachedAt != null) lastSync = formatSyncTimestamp(cachedAt)
        }
        try {
            val previousIds = works.mapTo(mutableSetOf()) { it.id }
            val synchronizedWorks = synchronizeMyWorks(token, username, workDao)
            val catalogCount = try { synchronizeMobileCatalog(token, catalogDao) } catch (_: Exception) { null }
            val sentReports = retryPendingReports(token, username, reportDao)
            val message = synchronizationNotice(synchronizedWorks.count { it.id !in previousIds }, catalogCount, sentReports)
            works = synchronizedWorks
            syncNotice = message
            launch {
                delay(10_000)
                if (syncNotice == message) syncNotice = null
            }
            lastSync = LocalDateTime.now().format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"))
        } catch (_: Exception) {
            if (works.isEmpty()) failed = true else offline = true
            syncNotice = connectionFailureNotice(works.isNotEmpty())
        } finally {
            loading = false
        }
    }

    Box(modifier = modifier.fillMaxSize()) {
    Column(modifier = Modifier.fillMaxSize().padding(20.dp)) {
        OutlinedButton(onClick = onBack) { Text("← Indietro", fontSize = 17.sp) }
        Spacer(modifier = Modifier.height(18.dp))
        Text("Lavori assegnati", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
        Spacer(modifier = Modifier.height(12.dp))
        Button(
            onClick = synchronize,
            enabled = !loading,
            modifier = Modifier.fillMaxWidth().height(52.dp)
        ) {
            Text(if (loading) "Sincronizzazione…" else "Sincronizza lavori", fontSize = 18.sp)
        }
        Text(
            text = if (lastSync.isBlank()) "Ultima sincronizzazione: non disponibile" else "Ultima sincronizzazione: $lastSync",
            style = MaterialTheme.typography.bodyMedium,
            modifier = Modifier.padding(top = 6.dp)
        )
        Spacer(modifier = Modifier.height(12.dp))
        val today = LocalDate.now().toString()
        val todayCount = works.count { it.plannedOn.take(10) == today }
        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            FilterChip(
                selected = !showAll,
                onClick = { showAll = false },
                label = { Text("Oggi ($todayCount)", fontSize = 17.sp) }
            )
            FilterChip(
                selected = showAll,
                onClick = { showAll = true },
                label = { Text("Tutti (${works.size})", fontSize = 17.sp) }
            )
        }
        Spacer(modifier = Modifier.height(16.dp))
        if (failed) Text("Impossibile sincronizzare i lavori", color = MaterialTheme.colorScheme.error, fontSize = 17.sp)
        if (offline) Text("Modalità offline — visualizzazione dell’ultima copia salvata", color = MaterialTheme.colorScheme.primary, fontSize = 16.sp)
        when {
            loading && works.isEmpty() -> CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally))
            works.isEmpty() -> Text("Nessun lavoro assegnato", fontSize = 18.sp)
            else -> {
                val orderedWorks = works.sortedWith(
                    compareBy<MobileWork> { it.dateKey }
                        .thenBy { it.plannedAt.ifBlank { "99:99:99" } }
                        .thenBy { it.id }
                )
                val visibleWorks = if (showAll) orderedWorks else orderedWorks.filter { it.plannedOn.take(10) == today }
                if (visibleWorks.isEmpty()) {
                    Text("Nessun lavoro previsto per oggi", fontSize = 18.sp)
                } else LazyColumn(state = worksListState, verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    visibleWorks.groupBy { it.dateKey }.forEach { (date, dayWorks) ->
                        item(key = "date-$date") {
                            Text(
                                text = dayWorks.first().dateLabel,
                                fontSize = 19.sp,
                                fontWeight = FontWeight.Bold,
                                color = MaterialTheme.colorScheme.primary,
                                modifier = Modifier.padding(top = 6.dp)
                            )
                        }
                        items(dayWorks, key = { it.id }) { work ->
                            Card(onClick = { selectedWorkId = work.id }, modifier = Modifier.fillMaxWidth()) {
                                Column(modifier = Modifier.padding(16.dp)) {
                                    Text("Scheda ${work.number}", fontSize = 18.sp, fontWeight = FontWeight.Bold)
                                    Text(work.timeLabel, fontSize = 17.sp)
                                    Spacer(modifier = Modifier.height(6.dp))
                                    Text(work.customer, fontSize = 19.sp, fontWeight = FontWeight.SemiBold)
                                    Text(work.site, fontSize = 16.sp)
                                    if (work.summary.isNotBlank()) Text(work.summary, fontSize = 17.sp, modifier = Modifier.padding(top = 6.dp))
                                }
                            }
                        }
                    }
                }
            }
        }
    }
        syncNotice?.let { notice ->
            Card(
                modifier = Modifier.align(Alignment.Center).padding(horizontal = 28.dp).fillMaxWidth(),
                border = BorderStroke(2.dp, MaterialTheme.colorScheme.primary),
                elevation = CardDefaults.cardElevation(defaultElevation = 8.dp),
                colors = CardDefaults.cardColors(containerColor = Color.White)
            ) {
                Column(
                    modifier = Modifier.padding(horizontal = 20.dp, vertical = 18.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(14.dp)
                ) {
                    Text(text = notice, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.fillMaxWidth())
                    Button(onClick = { syncNotice = null }, modifier = Modifier.fillMaxWidth()) { Text("OK") }
                }
            }
        }
    }
}

@Composable
private fun MobileAgendaScreen(username: String, token: String, onBack: () -> Unit, modifier: Modifier = Modifier) {
    val context = LocalContext.current
    val workDao = remember(context) { SkyLabDatabase.get(context).cachedWorks() }
    val catalogDao = remember(context) { SkyLabDatabase.get(context).mobileCatalog() }
    val reportDao = remember(context) { SkyLabDatabase.get(context).workReportDrafts() }
    val currentWeek = remember { LocalDate.now().with(TemporalAdjusters.previousOrSame(DayOfWeek.MONDAY)) }
    var weekStart by remember { mutableStateOf(currentWeek) }
    var works by remember { mutableStateOf<List<MobileWork>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var offline by remember { mutableStateOf(false) }
    var selectedWorkId by remember { mutableStateOf<Int?>(null) }
    var catalogNotice by remember { mutableStateOf<String?>(null) }

    BackHandler(onBack = onBack)
    selectedWorkId?.let { workId ->
        MobileWorkDetailScreen(
            username = username, token = token, workId = workId, workDao = workDao,
            onBack = { selectedWorkId = null }, modifier = modifier
        )
        return
    }

    LaunchedEffect(username, token) {
        val cached = loadCachedWorks(username, workDao)
        if (cached.isNotEmpty()) works = cached
        try {
            val previousIds = works.mapTo(mutableSetOf()) { it.id }
            val synchronizedWorks = synchronizeMyWorks(token, username, workDao)
            val catalogCount = try { synchronizeMobileCatalog(token, catalogDao) } catch (_: Exception) { null }
            val sentReports = retryPendingReports(token, username, reportDao)
            works = synchronizedWorks
            catalogNotice = synchronizationNotice(synchronizedWorks.count { it.id !in previousIds }, catalogCount, sentReports)
        } catch (_: Exception) {
            offline = works.isNotEmpty()
            catalogNotice = connectionFailureNotice(works.isNotEmpty())
        } finally {
            loading = false
        }
    }

    val weekEnd = weekStart.plusDays(6)
    val weekWorks = works.mapNotNull { work ->
        try { LocalDate.parse(work.plannedOn.take(10)) to work } catch (_: Exception) { null }
    }.filter { (date, _) -> !date.isBefore(weekStart) && !date.isAfter(weekEnd) }
        .sortedWith(compareBy<Pair<LocalDate, MobileWork>> { it.first }.thenBy { it.second.plannedAt }.thenBy { it.second.id })

    catalogNotice?.let { notice ->
        AlertDialog(
            onDismissRequest = { catalogNotice = null },
            containerColor = Color.White,
            title = { Text("Sincronizzazione completata") },
            text = { Text(notice) },
            confirmButton = { Button(onClick = { catalogNotice = null }) { Text("OK") } }
        )
    }

    Scaffold(
        modifier = modifier.fillMaxSize(),
        topBar = {
            Column(modifier = Modifier.padding(horizontal = 20.dp, vertical = 14.dp)) {
                OutlinedButton(onClick = onBack) { Text("← Indietro", fontSize = 17.sp) }
                Spacer(modifier = Modifier.height(10.dp))
                Text("Agenda", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
                Spacer(modifier = Modifier.height(8.dp))
                Row(
                    modifier = Modifier.fillMaxWidth(),
                    horizontalArrangement = Arrangement.SpaceBetween,
                    verticalAlignment = Alignment.CenterVertically
                ) {
                    Button(
                        onClick = { weekStart = weekStart.minusWeeks(1) },
                        modifier = Modifier.width(64.dp).height(48.dp),
                        contentPadding = PaddingValues(0.dp)
                    ) { Text("←", fontSize = 25.sp, fontWeight = FontWeight.Bold) }
                    OutlinedButton(
                        onClick = { weekStart = currentWeek },
                        enabled = weekStart != currentWeek,
                        modifier = Modifier.height(48.dp)
                    ) { Text("Oggi", fontSize = 17.sp) }
                    Button(
                        onClick = { weekStart = weekStart.plusWeeks(1) },
                        modifier = Modifier.width(64.dp).height(48.dp),
                        contentPadding = PaddingValues(0.dp)
                    ) { Text("→", fontSize = 25.sp, fontWeight = FontWeight.Bold) }
                }
                Text(
                    "${weekStart.format(DateTimeFormatter.ofPattern("dd/MM"))} – ${weekEnd.format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))}",
                    fontSize = 17.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.padding(top = 8.dp)
                )
                if (offline) Text("Modalità offline — ultima copia salvata", color = MaterialTheme.colorScheme.primary, fontSize = 15.sp)
            }
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(innerPadding).padding(horizontal = 20.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            when {
                loading && works.isEmpty() -> item { CircularProgressIndicator() }
                weekWorks.isEmpty() -> item { Text("Nessun lavoro nella settimana", fontSize = 18.sp) }
                else -> weekWorks.groupBy { it.first }.forEach { (_, entries) ->
                    item(key = "agenda-${entries.first().first}") {
                        Text(
                            entries.first().second.dateLabel,
                            color = MaterialTheme.colorScheme.primary,
                            fontSize = 19.sp,
                            fontWeight = FontWeight.Bold,
                            modifier = Modifier.padding(top = 6.dp)
                        )
                    }
                    items(entries, key = { it.second.id }) { (_, work) ->
                        Card(onClick = { selectedWorkId = work.id }, modifier = Modifier.fillMaxWidth()) {
                            Column(modifier = Modifier.padding(14.dp)) {
                                Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.SpaceBetween) {
                                    Text(work.timeLabel, fontSize = 18.sp, fontWeight = FontWeight.Bold)
                                    Text("Scheda ${work.number}", fontSize = 15.sp)
                                }
                                Text(work.customer, fontSize = 18.sp, fontWeight = FontWeight.SemiBold)
                                Text(work.site, fontSize = 15.sp)
                                if (work.summary.isNotBlank()) Text(work.summary, fontSize = 16.sp, modifier = Modifier.padding(top = 4.dp))
                            }
                        }
                    }
                }
            }
            item { Spacer(modifier = Modifier.height(24.dp)) }
        }
    }
}

@Composable
private fun MobileWorkDetailScreen(username: String, token: String, workId: Int, workDao: CachedWorkDao, onBack: () -> Unit, modifier: Modifier = Modifier) {
    var detail by remember(workId) { mutableStateOf<MobileWorkDetail?>(null) }
    var failed by remember(workId) { mutableStateOf(false) }
    var showReport by remember(workId) { mutableStateOf(false) }
    var reportDraft by remember(workId) { mutableStateOf<WorkReportDraftEntity?>(null) }
    val context = LocalContext.current
    val reportDao = remember(context) { SkyLabDatabase.get(context).workReportDrafts() }
    val catalogDao = remember(context) { SkyLabDatabase.get(context).mobileCatalog() }
    BackHandler(onBack = onBack)
    if (showReport && detail != null) {
        WorkReportScreen(username, token, detail!!, reportDao, catalogDao, onBack = { showReport = false }, modifier)
        return
    }
    LaunchedEffect(workId, token) {
        val cachedJson = workDao.detail(username, workId)
        if (!cachedJson.isNullOrBlank()) detail = parseMobileWorkDetail(cachedJson)
        try {
            val (freshDetail, json) = loadMobileWorkDetail(token, workId)
            detail = freshDetail
            workDao.updateDetail(username, workId, json, System.currentTimeMillis())
        } catch (exception: Exception) {
            failed = detail == null
            android.util.Log.e("SkyLabMobile", "Caricamento dettaglio non riuscito", exception)
        }
    }
    LaunchedEffect(workId, showReport) {
        if (!showReport) reportDraft = reportDao.draft(username, workId)
    }

    Scaffold(
        modifier = modifier.fillMaxSize(),
        topBar = {
            OutlinedButton(
                onClick = onBack,
                modifier = Modifier.padding(start = 20.dp, top = 20.dp, bottom = 10.dp)
            ) { Text("← Indietro", fontSize = 17.sp) }
        }
    ) { innerPadding ->
        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(innerPadding).padding(horizontal = 20.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
        when {
            failed -> item { Text("Impossibile caricare il dettaglio del lavoro", color = MaterialTheme.colorScheme.error, fontSize = 17.sp) }
            detail == null -> item { CircularProgressIndicator() }
            else -> {
                val work = detail!!
                item {
                    Text("Scheda ${work.number}", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
                    Text(work.status, color = MaterialTheme.colorScheme.primary, fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
                }
                item {
                    DetailSection("Pianificazione") {
                        DetailValue("Data scheda", formatMobileDate(work.draftedOn))
                        DetailValue("Data intervento", formatMobileDate(work.plannedOn))
                        DetailValue("Ora intervento", formatMobileTime(work.plannedAt))
                        DetailValue("Ultimo intervento", formatMobileDate(work.lastServiceOn))
                        DetailValue("Operatore", work.assignedOperator)
                        if (work.outcome.isNotBlank()) DetailValue("Esito", work.outcome)
                    }
                }
                item {
                    DetailSection("Cliente e sede") {
                        DetailValue("Cliente", work.customer)
                        DetailValue("Sede lavoro", work.site)
                    }
                }
                item {
                    DetailSection("Lavoro") {
                        DetailValue("Sintesi", work.summary.ifBlank { "Non specificata" })
                        DetailValue("Istruzioni operative", work.instructions.ifBlank { "Nessuna istruzione" })
                    }
                }
                item { DetailRowsSection("Prestazioni previste", work.services) }
                item { DetailRowsSection("Materiali previsti", work.materials) }
                item {
                    DetailSection("Preventivo") {
                        DetailValue("Manodopera", formatCurrency(work.plannedLabour))
                        DetailValue("Materiali", formatCurrency(work.plannedMaterials))
                        DetailValue("Totale netto", formatCurrency(work.plannedNet))
                    }
                }
                item {
                    reportDraft?.let {
                        Text(
                            when(it.status){"PENDING"->"Consuntivo confermato · in attesa di invio";"TRANSMITTED"->"Consuntivo trasmesso · in attesa di acquisizione";"ERROR"->"Consuntivo confermato · errore di invio";else->"Bozza consuntivo · ultima modifica ${formatSyncTimestamp(it.updatedAt)}"},
                            color = MaterialTheme.colorScheme.primary,
                            fontSize = 15.sp,
                            fontWeight = FontWeight.SemiBold,
                            modifier = Modifier.padding(bottom = 6.dp)
                        )
                    }
                    Button(
                        onClick = { showReport = true },
                        enabled = reportDraft?.status !in setOf("PENDING","TRANSMITTED","ERROR"),
                        modifier = Modifier.fillMaxWidth().height(54.dp)
                    ) {
                        Text(
                            when(reportDraft?.status){"PENDING"->"In attesa di invio";"TRANSMITTED"->"In attesa di acquisizione";"ERROR"->"Invio non riuscito";null->"Compila consuntivo";else->"Modifica consuntivo"},
                            fontSize = 18.sp
                        )
                    }
                }
                item { Spacer(modifier = Modifier.height(24.dp)) }
            }
        }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun WorkReportScreen(
    username: String,
    token: String,
    work: MobileWorkDetail,
    dao: WorkReportDraftDao,
    catalogDao: MobileCatalogDao,
    onBack: () -> Unit,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val lookupPreferences = remember(context) { context.getSharedPreferences("skylab-mobile-lookups", android.content.Context.MODE_PRIVATE) }
    var completedOn by remember(work.id) { mutableStateOf(LocalDate.now().toString()) }
    var completedAt by remember(work.id) { mutableStateOf(LocalDateTime.now().format(DateTimeFormatter.ofPattern("HH:mm"))) }
    var manHours by remember(work.id) { mutableStateOf("") }
    var outcome by remember(work.id) { mutableStateOf("") }
    var outcomes by remember { mutableStateOf(parseMobileOutcomes(lookupPreferences.getString("outcomes", "[]").orEmpty())) }
    var performed by remember(work.id) { mutableStateOf("") }
    var notes by remember(work.id) { mutableStateOf("") }
    var collected by remember(work.id) { mutableStateOf("") }
    var extraServices by remember(work.id) { mutableStateOf("") }
    var extraMaterials by remember(work.id) { mutableStateOf("") }
    var additionalServices by remember(work.id) { mutableStateOf(emptyList<AdditionalReportItem>()) }
    var additionalMaterials by remember(work.id) { mutableStateOf(emptyList<AdditionalReportItem>()) }
    var catalog by remember { mutableStateOf(emptyList<MobileCatalogEntity>()) }
    var selectedServices by remember(work.id) { mutableStateOf(work.services.indices.toSet()) }
    var selectedMaterials by remember(work.id) { mutableStateOf(work.materials.indices.toSet()) }
    var serviceQuantities by remember(work.id) { mutableStateOf(work.services.mapIndexed { i, row -> i to formatQuantity(row.quantity) }.toMap()) }
    var materialQuantities by remember(work.id) { mutableStateOf(work.materials.mapIndexed { i, row -> i to formatQuantity(row.quantity) }.toMap()) }
    var saved by remember(work.id) { mutableStateOf(false) }
    var showConfirm by remember(work.id) { mutableStateOf(false) }
    var sendNotice by remember(work.id) { mutableStateOf<String?>(null) }
    var draftNotice by remember(work.id) { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()
    val reportListState = rememberLazyListState()
    val focusManager = LocalFocusManager.current

    BackHandler(onBack = onBack)
    LaunchedEffect(work.id) {
        dao.draft(username, work.id)?.let { draft ->
            val json = JSONObject(draft.payloadJson)
            completedOn = json.optString("completedOn", completedOn)
            completedAt = json.optString("completedAt", completedAt)
            manHours = json.optString("manHours")
            outcome = json.optString("outcome")
            performed = json.optString("performed")
            notes = json.optString("notes")
            collected = json.optString("collected")
            extraServices = json.optString("extraServices")
            extraMaterials = json.optString("extraMaterials")
            additionalServices = parseAdditionalReportItems(json.optJSONArray("additionalServices"))
            additionalMaterials = parseAdditionalReportItems(json.optJSONArray("additionalMaterials"))
            selectedServices = json.optJSONArray("services")?.let { a -> (0 until a.length()).map { a.getInt(it) }.toSet() } ?: emptySet()
            selectedMaterials = json.optJSONArray("materials")?.let { a -> (0 until a.length()).map { a.getInt(it) }.toSet() } ?: emptySet()
            serviceQuantities = json.optJSONArray("serviceQuantities")?.let { a ->
                serviceQuantities.toMutableMap().apply { (0 until a.length()).forEach { put(it, a.optString(it, get(it).orEmpty())) } }
            } ?: serviceQuantities
            materialQuantities = json.optJSONArray("materialQuantities")?.let { a ->
                materialQuantities.toMutableMap().apply { (0 until a.length()).forEach { put(it, a.optString(it, get(it).orEmpty())) } }
            } ?: materialQuantities
        }
    }
    LaunchedEffect(token) {
        catalog = withContext(Dispatchers.IO) { catalogDao.items("A") + catalogDao.items("P") }
        try {
            val (freshOutcomes, json) = loadMobileOutcomes(token)
            outcomes = freshOutcomes
            lookupPreferences.edit().putString("outcomes", json).apply()
        } catch (exception: Exception) {
            android.util.Log.e("SkyLabMobile", "Caricamento esiti non riuscito", exception)
        }
    }

    fun draftJson() = JSONObject()
            .put("completedOn", completedOn).put("completedAt", completedAt)
            .put("manHours", manHours).put("outcome", outcome)
            .put("performed", performed).put("notes", notes).put("collected", collected)
            .put("extraServices", extraServices).put("extraMaterials", extraMaterials)
            .put("additionalServices", additionalReportItemsJson(additionalServices))
            .put("additionalMaterials", additionalReportItemsJson(additionalMaterials))
            .put("services", JSONArray(selectedServices.sorted()))
            .put("materials", JSONArray(selectedMaterials.sorted()))
            .put("serviceQuantities", JSONArray(work.services.indices.map { serviceQuantities[it].orEmpty() }))
            .put("materialQuantities", JSONArray(work.materials.indices.map { materialQuantities[it].orEmpty() }))

    fun transmissionJson(submissionId: String): JSONObject {
        val rows = JSONArray()
        selectedServices.sorted().forEach { index -> (parseReportNumber(serviceQuantities[index].orEmpty()) ?: 0.0).takeIf { it > 0 }?.let { rows.put(JSONObject().put("type","P").put("reference",work.services[index].reference).put("quantity",it).put("price",work.services[index].unitPrice)) } }
        selectedMaterials.sorted().forEach { index -> (parseReportNumber(materialQuantities[index].orEmpty()) ?: 0.0).takeIf { it > 0 }?.let { rows.put(JSONObject().put("type","A").put("reference",work.materials[index].reference).put("quantity",it).put("price",work.materials[index].unitPrice)) } }
        additionalServices.forEach { item -> (parseReportNumber(item.quantity) ?: 0.0).takeIf { it > 0 }?.let { rows.put(JSONObject().put("type","P").put("reference",item.reference).put("quantity",it).put("price",item.price)) } }
        additionalMaterials.forEach { item -> (parseReportNumber(item.quantity) ?: 0.0).takeIf { it > 0 }?.let { rows.put(JSONObject().put("type","A").put("reference",item.reference).put("quantity",it).put("price",item.price)) } }
        return JSONObject().put("submissionId",submissionId).put("completedOn",completedOn).put("completedAt",completedAt)
            .put("manHours",manHours).put("outcome",outcome).put("workPerformed",performed)
            .put("collectedAmount",parseCollectedAmount(collected)?:0.0).put("notes",notes).put("rows",rows)
    }

    fun saveDraft() {
        val json = draftJson()
        scope.launch {
            try {
                dao.save(WorkReportDraftEntity(username, work.id, json.toString(), System.currentTimeMillis()))
                saved = true
                draftNotice = "Bozza salvata sul dispositivo. Puoi continuare a modificarla."
            } catch (exception: Exception) {
                android.util.Log.e("SkyLabMobile", "Salvataggio bozza non riuscito", exception)
                draftNotice = "Impossibile salvare la bozza sul dispositivo."
            }
        }
    }

    fun confirmAndSend() {
        if(completedOn.isBlank()||completedAt.isBlank()||outcome.isBlank()) { sendNotice="Completare data, ora ed esito.";return }
        val submissionId=java.util.UUID.randomUUID().toString();val now=System.currentTimeMillis();val payload=transmissionJson(submissionId).toString();val stored=draftJson().put("_transmission",JSONObject(payload)).toString()
        scope.launch {
            var record=WorkReportDraftEntity(username,work.id,stored,now,"PENDING",submissionId,now,null,0,"");dao.save(record)
            try { sendMobileReport(token,work.id,payload);record=record.copy(status="TRANSMITTED",sentAt=System.currentTimeMillis(),attempts=1,lastError="");dao.save(record);sendNotice="Consuntivo trasmesso al server e in attesa di acquisizione." }
            catch(ex:MobileReportRejectedException){record=record.copy(status="ERROR",attempts=1,lastError=ex.message.orEmpty());dao.save(record);sendNotice="Consuntivo confermato. Invio non riuscito: ${ex.message}"}
            catch(_:Exception){record=record.copy(attempts=1,lastError="Connessione non disponibile");dao.save(record);sendNotice="Connessione non disponibile. Consuntivo confermato e salvato; invio in attesa."}
        }
    }

    Scaffold(
        modifier = modifier.fillMaxSize(),
        topBar = {
            Row(
                modifier = Modifier.fillMaxWidth().padding(horizontal = 20.dp, vertical = 14.dp),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically
            ) {
                OutlinedButton(onClick = onBack) { Text("← Indietro", fontSize = 16.sp) }
                Button(onClick = { saveDraft() }) { Text("Salva bozza", fontSize = 16.sp) }
            }
        }
    ) { innerPadding ->
        LazyColumn(
            state = reportListState,
            modifier = Modifier.fillMaxSize().padding(innerPadding).padding(horizontal = 20.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            item {
                Text("Consuntivo", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
                Text("Scheda ${work.number}", color = MaterialTheme.colorScheme.primary, fontSize = 17.sp)
                if (saved) Text("Bozza salvata sul dispositivo", color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.SemiBold)
            }
            item {
                DetailSection("Dati del lavoro") {
                    CompactDateField("Data lavoro", completedOn) { completedOn = it; saved = false }
                    CompactTimeField("Ora inizio", completedAt) { completedAt = it; saved = false }
                    CompactHoursField(manHours) { manHours = it; saved = false }
                    CompactOutcomeField(outcome, outcomes) { outcome = it; saved = false }
                    OutlinedTextField(performed, { performed = it; saved = false }, label = { Text("Attività svolte") }, minLines = 3, modifier = Modifier.fillMaxWidth())
                    Row(modifier = Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.End) {
                        TextButton(onClick = {
                            focusManager.clearFocus()
                            scope.launch { reportListState.animateScrollToItem(2) }
                        }) { Text("Avanti ↓") }
                    }
                }
            }
            item {
                ReportChecklist("Prestazioni previste", work.services, selectedServices, serviceQuantities,
                    onSelectionChange = { selectedServices = it; saved = false },
                    onQuantityChange = { index, value -> serviceQuantities = serviceQuantities + (index to value); saved = false })
            }
            item {
                AdditionalItemsSection("Prestazioni ulteriori", "P", work.priceList, catalog, additionalServices) {
                    additionalServices = it; saved = false
                }
            }
            item {
                ReportChecklist("Materiali previsti", work.materials, selectedMaterials, materialQuantities,
                    onSelectionChange = { selectedMaterials = it; saved = false },
                    onQuantityChange = { index, value -> materialQuantities = materialQuantities + (index to value); saved = false })
            }
            item {
                AdditionalItemsSection("Materiali ulteriori", "A", work.priceList, catalog, additionalMaterials) {
                    additionalMaterials = it; saved = false
                }
            }
            item {
                DetailSection("Chiusura") {
                    CompactCurrencyField(collected) { collected = it; saved = false }
                    OutlinedTextField(notes, { notes = it; saved = false }, label = { Text("Note consuntive") }, minLines = 2, modifier = Modifier.fillMaxWidth())
                }
            }
            item {
                Column(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                    OutlinedButton(onClick = { saveDraft() }, modifier = Modifier.fillMaxWidth().height(50.dp)) {
                        Text("Salva bozza", fontSize = 17.sp)
                    }
                    Button(onClick={showConfirm=true},modifier=Modifier.fillMaxWidth().height(54.dp)) {
                        Text("Conferma e invia",fontSize=18.sp)
                    }
                }
            }
            item { Spacer(modifier = Modifier.height(24.dp)) }
        }
    }
    if(showConfirm) AlertDialog(onDismissRequest={showConfirm=false},title={Text("Conferma consuntivo")},text={Text("Dopo la conferma il consuntivo non sarà più modificabile dal cellulare.")},confirmButton={Button(onClick={showConfirm=false;confirmAndSend()}){Text("Conferma e invia")}},dismissButton={TextButton(onClick={showConfirm=false}){Text("Annulla")}},containerColor=Color.White)
    draftNotice?.let { message -> AlertDialog(onDismissRequest={draftNotice=null},title={Text("Salvataggio bozza")},text={Text(message)},confirmButton={Button(onClick={draftNotice=null}){Text("OK")}},containerColor=Color.White) }
    sendNotice?.let { message -> AlertDialog(onDismissRequest={},title={Text("Consuntivo")},text={Text(message)},confirmButton={Button(onClick={sendNotice=null;onBack()}){Text("OK")}},containerColor=Color.White) }
}

@Composable
private fun CompactOutcomeField(
    value: String,
    outcomes: List<MobileOutcome>,
    onValueChange: (String) -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text("Esito", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
        Box(modifier = Modifier.weight(1f)) {
            OutlinedButton(
                onClick = { expanded = true },
                enabled = outcomes.isNotEmpty(),
                modifier = Modifier.fillMaxWidth().height(56.dp)
            ) {
                Text(
                    when {
                        value.isNotBlank() -> value
                        outcomes.isEmpty() -> "Esiti non disponibili"
                        else -> "Seleziona esito"
                    },
                    modifier = Modifier.weight(1f)
                )
                Text("▾")
            }
            DropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
                outcomes.forEach { item ->
                    DropdownMenuItem(
                        text = { Text(item.description) },
                        onClick = {
                            onValueChange(item.description)
                            expanded = false
                        }
                    )
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CompactDateField(label: String, value: String, onValueChange: (String) -> Unit) {
    var showPicker by remember { mutableStateOf(false) }
    val initialMillis = try {
        LocalDate.parse(value).atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli()
    } catch (_: Exception) {
        LocalDate.now().atStartOfDay(ZoneOffset.UTC).toInstant().toEpochMilli()
    }
    val pickerState = rememberDatePickerState(initialSelectedDateMillis = initialMillis)
    Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text(label, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
        OutlinedTextField(
            value = formatMobileDate(value),
            onValueChange = {},
            readOnly = true,
            singleLine = true,
            trailingIcon = { IconButton(onClick = { showPicker = true }) { Text("▣", fontSize = 21.sp) } },
            modifier = Modifier.weight(1f).clickable { showPicker = true }
        )
    }
    if (showPicker) {
        DatePickerDialog(
            onDismissRequest = { showPicker = false },
            confirmButton = {
                TextButton(onClick = {
                    pickerState.selectedDateMillis?.let { millis ->
                        onValueChange(Instant.ofEpochMilli(millis).atZone(ZoneOffset.UTC).toLocalDate().toString())
                    }
                    showPicker = false
                }) { Text("Conferma") }
            },
            dismissButton = { TextButton(onClick = { showPicker = false }) { Text("Annulla") } }
        ) { DatePicker(state = pickerState) }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun CompactTimeField(label: String, value: String, onValueChange: (String) -> Unit) {
    var showPicker by remember { mutableStateOf(false) }
    val parsed = value.split(":").mapNotNull { it.toIntOrNull() }
    val now = LocalDateTime.now()
    val initialHour = parsed.getOrNull(0)?.takeIf { it in 0..23 } ?: now.hour
    val initialMinute = parsed.getOrNull(1)?.takeIf { it in 0..59 } ?: now.minute
    val pickerState = rememberTimePickerState(initialHour = initialHour, initialMinute = initialMinute, is24Hour = true)
    Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text(label, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
        OutlinedTextField(
            value = value.takeIf { Regex("^(?:[01]\\d|2[0-3]):[0-5]\\d$").matches(it) }.orEmpty(),
            onValueChange = {},
            readOnly = true,
            singleLine = true,
            placeholder = { Text("hh:mm") },
            trailingIcon = { IconButton(onClick = { showPicker = true }) { Text("◷", fontSize = 24.sp) } },
            modifier = Modifier.weight(1f).clickable { showPicker = true }
        )
    }
    if (showPicker) {
        AlertDialog(
            onDismissRequest = { showPicker = false },
            confirmButton = {
                TextButton(onClick = {
                    onValueChange(String.format(Locale.ITALY, "%02d:%02d", pickerState.hour, pickerState.minute))
                    showPicker = false
                }) { Text("Conferma") }
            },
            dismissButton = { TextButton(onClick = { showPicker = false }) { Text("Annulla") } },
            text = { TimePicker(state = pickerState) }
        )
    }
}

@Composable
private fun CompactHoursField(value: String, onValueChange: (String) -> Unit) {
    fun numericValue(): Double = value.replace(',', '.').toDoubleOrNull() ?: 0.0
    fun setAdjusted(delta: Double) {
        val adjusted = (numericValue() + delta).coerceAtLeast(0.0)
        onValueChange(String.format(Locale.ITALY, "%.1f", adjusted))
    }
    Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text("Ore uomo", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
        OutlinedButton(
            onClick = { setAdjusted(-0.5) },
            modifier = Modifier.width(48.dp).height(48.dp),
            contentPadding = PaddingValues(0.dp)
        ) { Text("−", fontSize = 24.sp, fontWeight = FontWeight.Bold) }
        OutlinedTextField(
            value = value,
            onValueChange = { input ->
                if (input.isEmpty() || input.matches(Regex("^\\d{0,4}([,.]\\d{0,2})?$"))) onValueChange(input)
            },
            singleLine = true,
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
            modifier = Modifier.weight(1f).padding(horizontal = 6.dp)
        )
        Button(
            onClick = { setAdjusted(0.5) },
            modifier = Modifier.width(48.dp).height(48.dp),
            contentPadding = PaddingValues(0.dp)
        ) { Text("+", fontSize = 22.sp, fontWeight = FontWeight.Bold) }
    }
}

@Composable
private fun CompactReportField(
    label: String,
    value: String,
    onValueChange: (String) -> Unit,
    placeholder: String = "",
    keyboardType: KeyboardType = KeyboardType.Text
) {
    Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text(label, fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
        OutlinedTextField(
            value = value,
            onValueChange = onValueChange,
            singleLine = true,
            placeholder = if (placeholder.isBlank()) null else ({ Text(placeholder) }),
            keyboardOptions = KeyboardOptions(keyboardType = keyboardType),
            modifier = Modifier.weight(1f)
        )
    }
}

@Composable
private fun CompactCurrencyField(value: String, onValueChange: (String) -> Unit) {
    var focused by remember { mutableStateOf(false) }
    var displayValue by remember { mutableStateOf(formatCollectedAmount(value)) }

    LaunchedEffect(value, focused) {
        if (!focused) displayValue = formatCollectedAmount(value)
    }

    Row(modifier = Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
        Text("Importo incassato", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.width(104.dp))
        OutlinedTextField(
            value = displayValue,
            onValueChange = { input ->
                val cleaned = input.filter { it.isDigit() || it == ',' || it == '.' }
                displayValue = cleaned
                onValueChange(cleaned)
            },
            singleLine = true,
            placeholder = { Text("0,00") },
            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
            modifier = Modifier.weight(1f).onFocusChanged { state ->
                if (state.isFocused && !focused) {
                    displayValue = editableCollectedAmount(value)
                } else if (!state.isFocused && focused) {
                    displayValue = formatCollectedAmount(displayValue)
                    onValueChange(displayValue)
                }
                focused = state.isFocused
            }
        )
    }
}

@Composable
private fun AdditionalItemsSection(
    title: String,
    type: String,
    priceList: Int,
    catalog: List<MobileCatalogEntity>,
    items: List<AdditionalReportItem>,
    onItemsChange: (List<AdditionalReportItem>) -> Unit
) {
    var showCatalog by remember { mutableStateOf(false) }
    val available = catalog.filter { it.type == type }
    val total = items.sumOf { (parseReportNumber(it.quantity) ?: 0.0) * it.price }

    DetailSection(title) {
        if (items.isEmpty()) Text("Nessuna voce aggiunta", fontSize = 15.sp)
        items.forEachIndexed { index, item ->
            Column(modifier = Modifier.fillMaxWidth()) {
                Row(verticalAlignment = Alignment.Top) {
                    Column(modifier = Modifier.weight(1f)) {
                        Text("${item.reference} · ${item.description}", fontWeight = FontWeight.SemiBold, fontSize = 15.sp)
                        if (item.unit.isNotBlank()) Text("Unità: ${item.unit}", fontSize = 13.sp)
                    }
                    TextButton(onClick = { onItemsChange(items.filterIndexed { i, _ -> i != index }) }) { Text("Elimina") }
                }
                Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
                    OutlinedTextField(
                        value = item.quantity,
                        onValueChange = { quantity ->
                            val cleaned = quantity.filter { it.isDigit() || it == ',' || it == '.' }
                            onItemsChange(items.toMutableList().apply { set(index, item.copy(quantity = cleaned)) })
                        },
                        label = { Text("Quantità") },
                        singleLine = true,
                        keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                        modifier = Modifier.weight(1f)
                    )
                    Column(modifier = Modifier.weight(1f)) {
                        Text("Prezzo ${formatCurrency(item.price)}", fontSize = 14.sp)
                        Text("Totale ${formatCurrency((parseReportNumber(item.quantity) ?: 0.0) * item.price)}", fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                    }
                }
            }
        }
        if (items.isNotEmpty()) {
            Text("Totale aggiunte: ${formatCurrency(total)}", fontWeight = FontWeight.Bold, color = MaterialTheme.colorScheme.primary)
        }
        Button(onClick = { showCatalog = true }, enabled = available.isNotEmpty(), modifier = Modifier.fillMaxWidth()) {
            Text(if (available.isEmpty()) "Catalogo non disponibile" else "+ Aggiungi")
        }
    }

    if (showCatalog) {
        CatalogPickerDialog(
            title = if (type == "A") "Aggiungi materiale" else "Aggiungi prestazione",
            catalog = available.filter { candidate -> items.none { it.reference == candidate.reference } },
            onDismiss = { showCatalog = false },
            onSelect = { selected ->
                onItemsChange(items + AdditionalReportItem(selected.reference, selected.description, selected.unit, selected.priceFor(priceList), "1"))
                showCatalog = false
            }
        )
    }
}

@Composable
private fun CatalogPickerDialog(
    title: String,
    catalog: List<MobileCatalogEntity>,
    onDismiss: () -> Unit,
    onSelect: (MobileCatalogEntity) -> Unit
) {
    val context = LocalContext.current
    var search by remember { mutableStateOf("") }
    var scanMessage by remember { mutableStateOf<String?>(null) }
    val barcodeItems = remember(catalog) { catalog.filter { it.barcodes.isNotBlank() } }
    val scanner = remember(context) {
        val options = GmsBarcodeScannerOptions.Builder()
            .setBarcodeFormats(Barcode.FORMAT_EAN_13, Barcode.FORMAT_EAN_8, Barcode.FORMAT_CODE_128)
            .enableAutoZoom()
            .build()
        GmsBarcodeScanning.getClient(context, options)
    }
    val filtered = remember(catalog, search) {
        val term = search.trim()
        if (term.isBlank()) catalog.take(80)
        else catalog.filter {
                it.reference.contains(term, ignoreCase = true) ||
                it.description.contains(term, ignoreCase = true) ||
                it.category.contains(term, ignoreCase = true) ||
                it.barcodes.split('|').any { code -> code.equals(term, ignoreCase = true) }
        }.take(80)
    }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            Column(modifier = Modifier.fillMaxWidth()) {
                OutlinedTextField(search, { search = it }, label = { Text("Cerca") }, singleLine = true, modifier = Modifier.fillMaxWidth())
                if (barcodeItems.isNotEmpty()) {
                    Button(
                        onClick = {
                            scanMessage = null
                            scanner.startScan()
                                .addOnSuccessListener { barcode ->
                                    val value = barcode.rawValue.orEmpty().trim()
                                    val found = barcodeItems.firstOrNull { item -> item.barcodes.split('|').any { it == value } }
                                    if (found != null) onSelect(found) else scanMessage = "Barcode non presente nel Catalogo"
                                }
                                .addOnFailureListener { scanMessage = "Lettura barcode non disponibile" }
                        },
                        modifier = Modifier.fillMaxWidth().padding(top = 8.dp)
                    ) { Text("Scansiona barcode") }
                }
                scanMessage?.let { Text(it, color = MaterialTheme.colorScheme.error, modifier = Modifier.padding(top = 8.dp)) }
                Column(modifier = Modifier.fillMaxWidth().heightIn(max = 430.dp).verticalScroll(rememberScrollState())) {
                    filtered.forEach { item ->
                        Column(
                            modifier = Modifier.fillMaxWidth().clickable { onSelect(item) }.padding(vertical = 10.dp)
                        ) {
                            Text("${item.reference} · ${item.description}", fontWeight = FontWeight.SemiBold)
                            Text(listOf(item.category, item.unit, formatCurrency(item.price)).filter { it.isNotBlank() }.joinToString(" · "), fontSize = 13.sp)
                        }
                    }
                    if (filtered.isEmpty()) Text("Nessuna voce trovata", modifier = Modifier.padding(vertical = 16.dp))
                }
            }
        },
        confirmButton = {},
        dismissButton = { TextButton(onClick = onDismiss) { Text("Annulla") } }
    )
}

@Composable
private fun ReportChecklist(
    title: String,
    rows: List<MobileWorkDetailRow>,
    selected: Set<Int>,
    actualQuantities: Map<Int, String>,
    onSelectionChange: (Set<Int>) -> Unit,
    onQuantityChange: (Int, String) -> Unit
) {
    DetailSection(title) {
        if (rows.isEmpty()) Text("Nessuna voce preventivata", fontSize = 16.sp)
        rows.forEachIndexed { index, row ->
            Row(verticalAlignment = Alignment.Top) {
                Checkbox(
                    checked = index in selected,
                    onCheckedChange = { checked -> onSelectionChange(if (checked) selected + index else selected - index) }
                )
                Column(modifier = Modifier.padding(top = 6.dp).weight(1f)) {
                    Text(listOf(row.reference, row.description).filter { it.isNotBlank() }.joinToString(" · "), fontSize = 16.sp, fontWeight = FontWeight.SemiBold)
                    Row(
                        modifier = Modifier.fillMaxWidth().padding(top = 5.dp),
                        horizontalArrangement = Arrangement.spacedBy(8.dp)
                    ) {
                        OutlinedTextField(
                            value = formatQuantity(row.quantity),
                            onValueChange = {},
                            readOnly = true,
                            label = { Text("Q.tà prevista") },
                            modifier = Modifier.weight(1f)
                        )
                        OutlinedTextField(
                            value = actualQuantities[index].orEmpty(),
                            onValueChange = { onQuantityChange(index, it) },
                            enabled = index in selected,
                            label = { Text("Q.tà effettiva") },
                            keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                            modifier = Modifier.weight(1f)
                        )
                    }
                }
            }
        }
    }
}

@Composable
private fun DetailSection(title: String, content: @Composable () -> Unit) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(7.dp)) {
            Text(title, fontSize = 19.sp, fontWeight = FontWeight.Bold, color = MaterialTheme.colorScheme.primary)
            content()
        }
    }
}

@Composable
private fun DetailValue(label: String, value: String) {
    Row(verticalAlignment = Alignment.Top) {
        Text(
            label,
            style = MaterialTheme.typography.labelMedium,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.width(132.dp).padding(top = 3.dp)
        )
        Text(value.ifBlank { "—" }, fontSize = 17.sp)
    }
}

@Composable
private fun DetailRowsSection(title: String, rows: List<MobileWorkDetailRow>) {
    DetailSection(title) {
        if (rows.isEmpty()) {
            Text("Nessuna voce", fontSize = 17.sp)
        } else rows.forEach { row ->
            Column {
                Text(listOf(row.reference, row.description).filter { it.isNotBlank() }.joinToString(" · "), fontSize = 17.sp, fontWeight = FontWeight.SemiBold)
                Text("Quantità ${formatQuantity(row.quantity)} · ${formatCurrency(row.unitPrice)} · Totale ${formatCurrency(row.amount)}", fontSize = 15.sp)
            }
        }
    }
}

@Preview(showBackground = true)
@Composable
fun LoginScreenPreview() {
    SkyLabMobileTheme {
        LoginScreen(loadUsers = { listOf("tecnico") })
    }
}

private suspend fun loadLoginUsers(): List<String> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/login-users")
        .openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        if (connection.responseCode !in 200..299) {
            error("Risposta server ${connection.responseCode}")
        }
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        val array = JSONArray(json)
        List(array.length()) { index -> array.getString(index) }
    } finally {
        connection.disconnect()
    }
}

private suspend fun authenticate(username: String, password: String): String? = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/login")
        .openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "POST"
        connection.setRequestProperty("Content-Type", "application/json; charset=utf-8")
        connection.doOutput = true
        val body = JSONObject()
            .put("username", username)
            .put("password", password)
            .toString()
        connection.outputStream.use { it.write(body.toByteArray(Charsets.UTF_8)) }
        if (connection.responseCode !in 200..299) return@withContext null
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        JSONObject(json).getString("token")
    } finally {
        connection.disconnect()
    }
}

private suspend fun loadMobileOutcomes(token: String): Pair<List<MobileOutcome>, String> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/outcomes").openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Authorization", "Bearer $token")
        if (connection.responseCode !in 200..299) error("Risposta server ${connection.responseCode}")
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        parseMobileOutcomes(json) to json
    } finally {
        connection.disconnect()
    }
}

private fun parseMobileOutcomes(json: String): List<MobileOutcome> = try {
    val array = JSONArray(json.ifBlank { "[]" })
    List(array.length()) { index ->
        val item = array.getJSONObject(index)
        MobileOutcome(item.optInt("id"), item.optString("description"))
    }.filter { it.description.isNotBlank() }
} catch (_: Exception) {
    emptyList()
}

private data class MobileOutcome(val id: Int, val description: String)

private class MobileReportRejectedException(message: String): Exception(message)

private suspend fun sendMobileReport(token:String,workId:Int,payload:String)=withContext(Dispatchers.IO){
    val connection=URL("http://localhost:5187/api/mobile/my-works/$workId/report").openConnection() as HttpURLConnection
    try{connection.connectTimeout=10_000;connection.readTimeout=20_000;connection.requestMethod="POST";connection.doOutput=true;connection.setRequestProperty("Authorization","Bearer $token");connection.setRequestProperty("Content-Type","application/json; charset=utf-8");connection.outputStream.use{it.write(payload.toByteArray(Charsets.UTF_8))};if(connection.responseCode !in 200..299){val body=connection.errorStream?.bufferedReader()?.use{it.readText()}.orEmpty();val message=try{JSONObject(body).optString("error","Invio rifiutato dal server")}catch(_:Exception){"Invio rifiutato dal server"};throw MobileReportRejectedException(message)};val receipt=JSONObject(connection.inputStream.bufferedReader().use{it.readText()});val expected=JSONObject(payload).getString("submissionId");if(!receipt.optBoolean("received")||receipt.optString("status")!="RICEVUTO"||receipt.optString("submissionId")!=expected)throw java.io.IOException("Ricevuta server non valida") }finally{connection.disconnect()}
}

private suspend fun retryPendingReports(token:String,username:String,dao:WorkReportDraftDao):Int{
    var sent=0
    for(record in withContext(Dispatchers.IO){dao.pending(username)}){
        val payload=try{JSONObject(record.payloadJson).optJSONObject("_transmission")?.toString().orEmpty()}catch(_:Exception){""}
        if(payload.isBlank())continue
        try{sendMobileReport(token,record.workId,payload);dao.save(record.copy(status="TRANSMITTED",sentAt=System.currentTimeMillis(),attempts=record.attempts+1,lastError=""));sent++}
        catch(ex:MobileReportRejectedException){dao.save(record.copy(status="ERROR",attempts=record.attempts+1,lastError=ex.message.orEmpty()))}
        catch(_:Exception){dao.save(record.copy(attempts=record.attempts+1,lastError="Connessione non disponibile"));break}
    }
    return sent
}

private fun JSONObject.optNullableDouble(name: String): Double? =
    if (isNull(name)) null else optDouble(name).takeUnless { it.isNaN() }

private suspend fun loadMobileCatalog(token: String): List<MobileCatalogEntity> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/catalog").openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 10_000
        connection.readTimeout = 20_000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Authorization", "Bearer $token")
        if (connection.responseCode !in 200..299) error("Risposta server ${connection.responseCode}")
        val array = JSONArray(connection.inputStream.bufferedReader().use { it.readText() })
        val synchronizedAt = System.currentTimeMillis()
        List(array.length()) { index ->
            val item = array.getJSONObject(index)
            MobileCatalogEntity(
                type = item.optString("type"),
                reference = item.optString("reference"),
                description = item.optString("description"),
                category = item.optString("category"),
                unit = item.optString("unit"),
                price = item.optDouble("price", 0.0),
                price1 = item.optNullableDouble("price1"),
                price2 = item.optNullableDouble("price2"),
                price3 = item.optNullableDouble("price3"),
                price4 = item.optNullableDouble("price4"),
                price5 = item.optNullableDouble("price5"),
                price6 = item.optNullableDouble("price6"),
                barcodes = item.optString("barcodes"),
                synchronizedAt = synchronizedAt
            )
        }.filter { it.type in setOf("A", "P") && it.reference.isNotBlank() }
    } finally {
        connection.disconnect()
    }
}

private suspend fun synchronizeMobileCatalog(token: String, dao: MobileCatalogDao): Int {
    val fresh = loadMobileCatalog(token)
    val current = withContext(Dispatchers.IO) { dao.items("A") + dao.items("P") }
    val currentByKey = current.associateBy { it.type to it.reference }
    val freshByKey = fresh.associateBy { it.type to it.reference }
    val changed = (currentByKey.keys + freshByKey.keys).count { key ->
        val old = currentByKey[key]
        val new = freshByKey[key]
        old == null || new == null ||
            old.description != new.description || old.category != new.category ||
            old.unit != new.unit || old.price != new.price ||
            old.price1 != new.price1 || old.price2 != new.price2 || old.price3 != new.price3 ||
            old.price4 != new.price4 || old.price5 != new.price5 || old.price6 != new.price6 || old.barcodes != new.barcodes
    }
    if (changed > 0) withContext(Dispatchers.IO) { dao.replaceAll(fresh) }
    return changed
}

private data class AdditionalReportItem(
    val reference: String,
    val description: String,
    val unit: String,
    val price: Double,
    val quantity: String
)

private fun MobileCatalogEntity.priceFor(listNumber: Int): Double = when (listNumber) {
    1 -> price1
    2 -> price2
    3 -> price3
    4 -> price4
    5 -> price5
    6 -> price6
    else -> null
}?.takeIf { it > 0.0 } ?: price

private fun additionalReportItemsJson(items: List<AdditionalReportItem>) = JSONArray().apply {
    items.forEach { item ->
        put(JSONObject()
            .put("reference", item.reference)
            .put("description", item.description)
            .put("unit", item.unit)
            .put("price", item.price)
            .put("quantity", item.quantity))
    }
}

private fun parseAdditionalReportItems(array: JSONArray?): List<AdditionalReportItem> {
    if (array == null) return emptyList()
    return (0 until array.length()).mapNotNull { index ->
        val item = array.optJSONObject(index) ?: return@mapNotNull null
        AdditionalReportItem(
            reference = item.optString("reference"),
            description = item.optString("description"),
            unit = item.optString("unit"),
            price = item.optDouble("price", 0.0),
            quantity = item.optString("quantity", "1")
        ).takeIf { it.reference.isNotBlank() }
    }
}

private data class MobileWork(
    val id: Int,
    val number: String,
    val plannedOn: String,
    val plannedAt: String,
    val customer: String,
    val site: String,
    val summary: String,
    val status: String
) {
    val dateKey: String get() = plannedOn.take(10).ifBlank { "9999-12-31" }
    val timeLabel: String get() = if (plannedAt.length >= 5) plannedAt.substring(0, 5) else "Ora da definire"
    val dateLabel: String
        get() = try {
            LocalDate.parse(plannedOn.take(10)).format(DateTimeFormatter.ofPattern("EEEE d MMMM yyyy", Locale.ITALIAN))
                .replaceFirstChar { it.uppercase() }
        } catch (_: Exception) {
            "Data da definire"
        }
}

private data class MobileWorkDetailRow(
    val reference: String,
    val description: String,
    val quantity: Double,
    val unitPrice: Double,
    val amount: Double
)

private data class MobileWorkDetail(
    val id: Int,
    val number: String,
    val draftedOn: String,
    val plannedOn: String,
    val plannedAt: String,
    val lastServiceOn: String,
    val customer: String,
    val site: String,
    val priceList: Int,
    val assignedOperator: String,
    val status: String,
    val outcome: String,
    val summary: String,
    val instructions: String,
    val plannedLabour: Double,
    val plannedMaterials: Double,
    val plannedNet: Double,
    val services: List<MobileWorkDetailRow>,
    val materials: List<MobileWorkDetailRow>
)

private fun formatMobileDate(value: String): String = try {
    LocalDate.parse(value.take(10)).format(DateTimeFormatter.ofPattern("dd/MM/yyyy"))
} catch (_: Exception) { "—" }

private fun formatMobileTime(value: String): String = if (value.length >= 5) value.substring(0, 5) else "—"
private fun formatCurrency(value: Double): String = NumberFormat.getCurrencyInstance(Locale.ITALY).format(value)
private fun parseCollectedAmount(value: String): Double? {
    val cleaned = value.replace("€", "").replace("\u00A0", "").replace(" ", "").trim()
    if (cleaned.isBlank()) return null
    val decimal = when {
        cleaned.contains(',') -> cleaned.replace(".", "").replace(',', '.')
        cleaned.count { it == '.' } > 1 -> cleaned.replace(".", "")
        else -> cleaned
    }
    return decimal.toDoubleOrNull()
}
private fun parseReportNumber(value: String): Double? {
    val cleaned = value.trim().replace(" ", "")
    if (cleaned.isBlank()) return null
    return when {
        cleaned.contains(',') -> cleaned.replace(".", "").replace(',', '.')
        else -> cleaned
    }.toDoubleOrNull()
}
private fun formatCollectedAmount(value: String): String = parseCollectedAmount(value)?.let(::formatCurrency).orEmpty()
private fun editableCollectedAmount(value: String): String = parseCollectedAmount(value)?.let {
    java.math.BigDecimal.valueOf(it).stripTrailingZeros().toPlainString().replace('.', ',')
}.orEmpty()
private fun formatQuantity(value: Double): String = NumberFormat.getNumberInstance(Locale.ITALY).apply { maximumFractionDigits = 3 }.format(value)
private fun formatSyncTimestamp(value: Long): String = LocalDateTime.ofInstant(Instant.ofEpochMilli(value), ZoneId.systemDefault())
    .format(DateTimeFormatter.ofPattern("dd/MM/yyyy HH:mm:ss"))

private fun synchronizationNotice(newWorks: Int, catalogChanges: Int?, sentReports: Int = 0): String {
    val worksText = "Elenco lavori aggiornato:\n    $newWorks ${if (newWorks == 1) "nuovo lavoro" else "nuovi lavori"}"
    val catalogText = when (catalogChanges) {
        null -> "Catalogo non aggiornato"
        1 -> "Catalogo aggiornato:\n    1 variazione"
        else -> "Catalogo aggiornato:\n    $catalogChanges variazioni"
    }
    val reportsText=if(sentReports>0)"\n\nConsuntivi trasmessi:\n    $sentReports" else ""
    return "$worksText\n\n$catalogText$reportsText"
}

private fun connectionFailureNotice(hasLocalCopy: Boolean): String =
    if (hasLocalCopy) "Connessione non disponibile, impossibile aggiornare.\nÈ visualizzata l’ultima copia salvata."
    else "Connessione non disponibile, impossibile aggiornare.\nNessun dato locale disponibile."

private fun CachedWorkEntity.toMobileWork() = MobileWork(workId, number, plannedOn, plannedAt, customer, site, summary, status)

private fun MobileWork.toCache(username: String, detailJson: String?) = CachedWorkEntity(
    username = username, workId = id, number = number, plannedOn = plannedOn, plannedAt = plannedAt,
    customer = customer, site = site, summary = summary, status = status, detailJson = detailJson,
    synchronizedAt = System.currentTimeMillis()
)

private const val MOBILE_WORK_RETENTION_DAYS = 60L

private fun mobileWorkCutoff(): LocalDate = LocalDate.now().minusDays(MOBILE_WORK_RETENTION_DAYS)

private fun isInsideMobileRetention(plannedOn: String, cutoff: LocalDate): Boolean = try {
    !LocalDate.parse(plannedOn.take(10)).isBefore(cutoff)
} catch (_: Exception) {
    true
}

private suspend fun loadCachedWorks(username: String, dao: CachedWorkDao): List<MobileWork> =
    withContext(Dispatchers.IO) {
        dao.deleteOlderThan(username, mobileWorkCutoff().toString())
        dao.works(username).map { it.toMobileWork() }
    }

private suspend fun synchronizeMyWorks(token: String, username: String, dao: CachedWorkDao): List<MobileWork> {
    val cutoff = mobileWorkCutoff()
    val existing = withContext(Dispatchers.IO) {
        dao.deleteOlderThan(username, cutoff.toString())
        dao.works(username)
    }
    val existingById = existing.associateBy { it.workId }
    val remoteWorks = loadMyWorks(token)
    val retainedItems = existing.associateByTo(linkedMapOf()) { it.workId }
    remoteWorks.filter { isInsideMobileRetention(it.plannedOn, cutoff) }.forEach { work ->
        val detailJson = try {
            loadMobileWorkDetail(token, work.id).second
        } catch (_: Exception) {
            existingById[work.id]?.detailJson
        }
        retainedItems[work.id] = work.toCache(username, detailJson)
    }
    withContext(Dispatchers.IO) {
        dao.replaceForUser(username, retainedItems.values.toList())
    }
    return retainedItems.values.map { it.toMobileWork() }
}

private suspend fun loadMyWorks(token: String): List<MobileWork> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/my-works").openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Authorization", "Bearer $token")
        if (connection.responseCode !in 200..299) error("Risposta server ${connection.responseCode}")
        val array = JSONArray(connection.inputStream.bufferedReader().use { it.readText() })
        List(array.length()) { index ->
            val item = array.getJSONObject(index)
            MobileWork(
                id = item.getInt("id"),
                number = item.getString("number"),
                plannedOn = item.optString("plannedOn"),
                plannedAt = item.optString("plannedAt"),
                customer = item.getString("customer"),
                site = item.getString("site"),
                summary = item.getString("summary"),
                status = item.getString("status")
            )
        }
    } finally {
        connection.disconnect()
    }
}

private suspend fun loadMobileWorkDetail(token: String, workId: Int): Pair<MobileWorkDetail, String> = withContext(Dispatchers.IO) {
    val connection = URL("http://localhost:5187/api/mobile/my-works/$workId").openConnection() as HttpURLConnection
    try {
        connection.connectTimeout = 5_000
        connection.readTimeout = 5_000
        connection.requestMethod = "GET"
        connection.setRequestProperty("Authorization", "Bearer $token")
        if (connection.responseCode !in 200..299) error("Risposta server ${connection.responseCode}")
        val json = connection.inputStream.bufferedReader().use { it.readText() }
        parseMobileWorkDetail(json) to json
    } finally {
        connection.disconnect()
    }
}

private fun parseMobileWorkDetail(json: String): MobileWorkDetail {
    val item = JSONObject(json)
    fun rows(name: String): List<MobileWorkDetailRow> {
        val array = item.getJSONArray(name)
        return List(array.length()) { index ->
            val row = array.getJSONObject(index)
            MobileWorkDetailRow(
                reference = row.optString("reference"), description = row.optString("description"),
                quantity = row.optDouble("quantity"), unitPrice = row.optDouble("unitPrice"), amount = row.optDouble("amount")
            )
        }
    }
    return MobileWorkDetail(
        id = item.getInt("id"), number = item.getString("number"),
        draftedOn = item.optString("draftedOn"), plannedOn = item.optString("plannedOn"), plannedAt = item.optString("plannedAt"),
        lastServiceOn = item.optString("lastServiceOn"), customer = item.optString("customer"), site = item.optString("site"), priceList = item.optInt("priceList", 0),
        assignedOperator = item.optString("assignedOperator"), status = item.optString("status"), outcome = item.optString("outcome"),
        summary = item.optString("summary"), instructions = item.optString("instructions"),
        plannedLabour = item.optDouble("plannedLabour"), plannedMaterials = item.optDouble("plannedMaterials"), plannedNet = item.optDouble("plannedNet"),
        services = rows("services"), materials = rows("materials")
    )
}
