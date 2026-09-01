package it.skylab.mobile

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.imePadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.FilterChip
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.tooling.preview.Preview
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.DpOffset
import androidx.compose.ui.unit.sp
import it.skylab.mobile.ui.theme.SkyLabMobileTheme
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.net.HttpURLConnection
import java.net.URL
import java.time.LocalDate
import java.time.format.DateTimeFormatter
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
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var passwordVisible by remember { mutableStateOf(false) }
    var menuExpanded by remember { mutableStateOf(false) }
    var users by remember { mutableStateOf<List<String>>(emptyList()) }
    var loadingUsers by remember { mutableStateOf(true) }
    var usersError by remember { mutableStateOf(false) }
    var loginInProgress by remember { mutableStateOf(false) }
    var loginError by remember { mutableStateOf(false) }
    var loggedIn by remember { mutableStateOf(false) }
    var sessionToken by remember { mutableStateOf("") }
    val scope = rememberCoroutineScope()

    LaunchedEffect(Unit) {
        try {
            users = loadUsers()
        } catch (_: Exception) {
            usersError = true
        } finally {
            loadingUsers = false
        }
    }

    if (loggedIn) {
        WelcomeScreen(username = username, token = sessionToken, modifier = modifier)
        return
    }

    Column(
        modifier = modifier.fillMaxSize().imePadding().padding(horizontal = 32.dp),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Text(
            text = "SkyLab Mobile",
            style = MaterialTheme.typography.headlineLarge,
            fontSize = 30.sp,
            fontWeight = FontWeight.Bold
        )
        Text(
            text = "Accedi al tuo account",
            style = MaterialTheme.typography.bodyLarge,
            fontSize = 17.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant
        )
        Spacer(modifier = Modifier.height(32.dp))
        Box(modifier = Modifier.fillMaxWidth()) {
            OutlinedButton(
                onClick = { menuExpanded = true },
                modifier = Modifier.fillMaxWidth().height(56.dp),
                enabled = !loadingUsers && users.isNotEmpty()
            ) {
                if (loadingUsers) {
                    CircularProgressIndicator(modifier = Modifier.height(20.dp))
                } else {
                    Text(username.ifEmpty { "Seleziona utente" }, fontSize = 20.sp)
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
            modifier = Modifier.fillMaxWidth(),
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
        Spacer(modifier = Modifier.height(24.dp))
        Button(
            onClick = {
                loginError = false
                loginInProgress = true
                scope.launch {
                    try {
                        sessionToken = authenticate(username, password).orEmpty()
                        loggedIn = sessionToken.isNotEmpty()
                        loginError = !loggedIn
                    } catch (_: Exception) {
                        loginError = true
                    } finally {
                        loginInProgress = false
                    }
                }
            },
            modifier = Modifier.fillMaxWidth(),
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
private fun WelcomeScreen(username: String, token: String, modifier: Modifier = Modifier) {
    var showWorks by remember { mutableStateOf(false) }
    if (showWorks) {
        MyWorksScreen(token = token, onBack = { showWorks = false }, modifier = modifier)
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
            Text("I miei lavori", fontSize = 20.sp)
        }
        Spacer(modifier = Modifier.height(16.dp))
        OutlinedButton(
            onClick = { },
            modifier = Modifier.fillMaxWidth().height(58.dp)
        ) {
            Text("Agenda", fontSize = 20.sp)
        }
    }
}

@Composable
private fun MyWorksScreen(token: String, onBack: () -> Unit, modifier: Modifier = Modifier) {
    var works by remember { mutableStateOf<List<MobileWork>>(emptyList()) }
    var loading by remember { mutableStateOf(true) }
    var failed by remember { mutableStateOf(false) }
    var showAll by remember { mutableStateOf(false) }

    LaunchedEffect(token) {
        try {
            works = loadMyWorks(token)
        } catch (_: Exception) {
            failed = true
        } finally {
            loading = false
        }
    }

    Column(modifier = modifier.fillMaxSize().padding(20.dp)) {
        OutlinedButton(onClick = onBack) { Text("← Indietro", fontSize = 17.sp) }
        Spacer(modifier = Modifier.height(18.dp))
        Text("I miei lavori", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold)
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
        when {
            loading -> CircularProgressIndicator(modifier = Modifier.align(Alignment.CenterHorizontally))
            failed -> Text("Impossibile caricare i lavori", color = MaterialTheme.colorScheme.error, fontSize = 17.sp)
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
                } else LazyColumn(verticalArrangement = Arrangement.spacedBy(12.dp)) {
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
                            Card(modifier = Modifier.fillMaxWidth()) {
                                Column(modifier = Modifier.padding(16.dp)) {
                                    Text("Scheda ${work.number}", fontSize = 18.sp, fontWeight = FontWeight.Bold)
                                    Text(work.timeLabel, fontSize = 17.sp)
                                    Spacer(modifier = Modifier.height(6.dp))
                                    Text(work.customer, fontSize = 19.sp, fontWeight = FontWeight.SemiBold)
                                    Text(work.site, fontSize = 16.sp)
                                    if (work.summary.isNotBlank()) Text(work.summary, fontSize = 17.sp, modifier = Modifier.padding(top = 6.dp))
                                    Text(work.status, color = MaterialTheme.colorScheme.primary, fontSize = 16.sp, modifier = Modifier.padding(top = 8.dp))
                                }
                            }
                        }
                    }
                }
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
