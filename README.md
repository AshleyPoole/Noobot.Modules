# Noobot.Modules
List of open source modules for the Noobot framework.

<br>

## NewRelic Module
Allows listing of applications on configured NewRelic account, getting health summary of all applications or just a single application.

Nuget Package - https://www.nuget.org/packages/Noobot.Modules.NewRelic/

### Example Command(s)
`@bot newrelic applications summary`

### Configuration
Example bot configuration:
```
	"Bot": {
		"newrelic:apikey": "YOUR_API_KEY",
		"slack:apiToken": "YOUR_API_KEY",
	}
```

<br>

## LoadBalancer.Org Module
Allows editing of a RIP on the load balancer, changing it's state using the `drain`, `halt` and `online` commands.

Nuget Package - https://www.nuget.org/packages/Noobot.Modules.LoadBalancerDotOrg/

### Example Command(s)
`@bot lbo myappliance drain vip01 rip01`

### Configuration
Due to the framework doesn't currently support sub configuration sections under Bot, you have to cheat with a JSON string per appliance. When executing commands for thi module, it looks for a appliance with a matching element. For example, if your appliance is called `myappliance`, an element called `lbo:myappliance` must exist.
Example bot configuration:
```
	"Bot": {
		"slack:apiToken": "YOUR_API_KEY",
		"lbo:trustAllCerts": "true",
		"lbo:myappliance": "{\"Name\": \"myappliance\",\"Username\": \"loadbalancer\",\"Password\": \"loadbalancer\",\"ApiUrl\": \"https://YOUR_APPLIANCE_NAME:9443/api/\",\"ApiKey\": \"YOUR_API_KEY\"}"
	}
```

<br>

## DNS Module
Allows simply DNS resolution using the systems configured DNS servers.

Nuget Package - https://www.nuget.org/packages/Noobot.Modules.DNS/

### Example Command(s)
`@bot dns lookup www.ashleypoole.co.uk`

### Configuration
Nothing special required at this time.

<br>

## Incident Module
Allows editing of a RIP on the load balancer, changing it's state using the `drain`, `halt` and `online` commands.

### Example Command(s)
`@bot incident new The web server is offline`

### Configuration
Due to the framework doesn't currently support sub configuration sections under Bot, you have to cheat with a JSON string per appliance. When executing commands for thi module, it looks for a appliance with a matching element. For example, if your appliance is called `myappliance`, an element called `lbo:myappliance` must exist.
Example bot configuration:
```
	"Bot": {
		"slack:apiToken": "YOUR_API_KEY",
		"incident:mainChannel": "incidents",
		"incident:azureConnectionString": "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT_NAME;AccountKey=YOUR_ACCOUNT_KEY;EndpointSuffix=YOUR_ENDPOINT_SUFFIX"
	}
```