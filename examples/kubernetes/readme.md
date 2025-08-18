# Running PlexSSO on kubernetes with ingress-nginx

This assumes you have a kubernetes environment set up, and [ingress-nginx](https://kubernetes.github.io/ingress-nginx/) is working properly. You also likely want [cert-manager](https://cert-manager.io/) or similar to handle HTTPS certificates. Due to the way this works, your PlexSSO instance is publicly available (at least as available as whatever endpoint you want to use PlexSSO to protect), so HTTPS is basically a requirement.

## PlexSSO

Install largely matches what you do for docker, but just in kubernetes style. You want a persistent volume to hold the configuration directory. You could make the configuration file an Opaque kubernetes Secret, however the PlexSSO program generates a long-lived token for its use, and if you don't have a persistent config directory, it will have to generate a new one on every restart of the pod, which might make Plex sad.

The below examples are genericised versions of [ejstacey's plexsso config](https://repo.joyrex.net/ejstacey/kube-configs/src/branch/main/infra/plexsso).

Start with the PVC. You will likely have to adapt this for your storage backend config. You don't need much space.

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: plexsso-config
spec:
  accessModes:
  - ReadWriteOnce
  resources:
    requests:
      storage: 1Mi
  storageClassName: longhorn-static
  volumeMode: Filesystem
  volumeName: plexsso-config
```

Next is the simple service file:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: plexsso
spec:
  ports:
    - port: 4200
      targetPort: 4200
      protocol: TCP
  selector:
    io.kompose.service: plexsso
```

As mentioned, the service needs to be publicly available, so an ingress is needed. You will want to add appropriate annotations for ssl termination (not shown):

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: plexsso
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - plexsso.my.domain
    secretName: plexsso-my-domain-tls
  rules:
  - host: plexsso.my.domain
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: plexsso
            port:
              number: 4200
```

Finally, the deployment.

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  labels:
    io.kompose.service: plexsso
  name: plexsso
spec:
  replicas: 1
  selector:
    matchLabels:
      io.kompose.service: plexsso
  strategy:
    type: Recreate
  template:
    metadata:
      labels:
        io.kompose.service: plexsso
    spec:
      containers:
        - env:
            - name: TZ
              value: Australia/Melbourne
          image: docker.io/drkno/plexsso:latest
          imagePullPolicy: IfNotPresent
          name: plexsso
          stdin: true
          tty: true
          volumeMounts:
            - mountPath: /config
              name: plexsso-config
          resources:
            requests:
              cpu: 10m
              memory: 300M
            limits:
              cpu: 100m
              memory: 1G
      hostname: plexsso
      restartPolicy: Always
      volumes:
        - name: plexsso-config
          persistentVolumeClaim:
            claimName: plexsso-config
```

This deployment uses no command line arguments, relying on the config file for everything. You can either run the deployment once to generate the generic config.json file and edit it, or just create your own and place it in the config pv ahead of time. See [the root readme.md](https://github.com/drkno/PlexSSO/blob/main/Readme.md) for details of the settings for the file.

Example config.json (serverIdentifier is faked):

```js
{
  "configFile": "/config/",
  "pluginDirectory": "/app",
  "serverIdentifier": "dd162ea38baedce9375684bf55b8046deed8693e7",
  "plexPreferencesFile": null,
  "cookieDomain": ".my.domain",
  "defaultAccessDeniedMessage": "Access to server denied.",
  "accessControls": {
    "eic": [
      {
        "path": "/",
        "minimumAccessTier": "NormalUser",
        "controlType": "Block",
        "exempt": [],
        "blockMessage": "You don't have permission for this."
      }
    ]
  },
  "plugins": {}
}
```

What's important with config.json is you want the service name ('eic' in the example above) to match the subdomain of the resource you want to protect. The cookie domain gets attached to the service name, so a service name of "eic" will be used to protect `https://eic.my.domain`.

## Protected Resource

We now have a generic plexsso instance that can be used for any service that runs off its own subdomain.

Then, to protect a resource (like `https://eic.my.domain`), add some annotations to the ingress for the resource being protected. Below is a partial ingress entry:

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  annotations:
    nginx.ingress.kubernetes.io/auth-url: https://plexsso.my.domain/api/v2/sso
    nginx.ingress.kubernetes.io/auth-signin: https://plexsso.my.domain/eic
  name: eic-my-domain
spec:
  ingressClassName: nginx
```

Note the following things:

1. auth-url has to point to api/v2/sso on the plexsso instance used. This is the endpoint that tests if you're already authed.
2. auth-signin url has to point to a subdirectory that matches the service name in plexsso's config.json. This value is then used to test against the service name and redirect you back to your proper subdomain on successful authentication and authorisation.

At this point you should be able to visit your protected service, get redirected to the plexsso instance, log into plex if necessary, and then be redirected back to your protected service. To test failures, edit the plexsso instance's config.json and add your plex username to the "exempt" array for the service to make sure it's blocking how you expect (this will require a restart of the plexsso pod).
