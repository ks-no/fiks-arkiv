pipeline {
    agent any
    environment {
        PROJECT_ARKIVSYSTEM_FOLDER = "ks.fiks.io.arkivsystem.sample"
        PROJECT_FAGSYSTEM_ARKIV_FOLDER = "ks.fiks.io.fagsystem.arkiv.sample"
        PROJECT_COMMON_FOLDER = "dotnet-source/ks.fiks.io.arkivintegrasjon.common"
        PROJECT_CHARTNAME = "fiks-arkiv-simulator"
        ARKIVSYSTEM_APP_NAME = "fiks-arkiv-simulator-arkivsystem"
        FAGSYSTEM_ARKIV_APP_NAME = "fiks-arkiv-simulator-fagsystem-arkiv"
        DOCKERFILE_TESTS = "Dockerfile-run-tests"
        // Artifactory credentials is stored under this key
        ARTIFACTORY_CREDENTIALS = "artifactory-token-based"
        // URL to artifactory Docker release repo
        DOCKER_REPO_RELEASE = "https://docker-all.artifactory.fiks.ks.no"
        // URL to artifactory Docker Snapshot repo
        DOCKER_REPO = "https://docker-local-snapshots.artifactory.fiks.ks.no"
    }
    parameters {
        booleanParam(defaultValue: false, description: 'Skal prosjektet releases?', name: 'isRelease')
        string(name: "specifiedVersion", defaultValue: "", description: "Hva er det nye versjonsnummeret (X.X.X)? Som default releases snapshot-versjonen")
        text(name: "releaseNotes", defaultValue: "Ingen endringer utført", description: "Hva er endret i denne releasen?")
        text(name: "securityReview", defaultValue: "Endringene har ingen sikkerhetskonsekvenser", description: "Har endringene sikkerhetsmessige konsekvenser, og hvilke tiltak er i så fall iverksatt?")
        string(name: "reviewer", defaultValue: "Endringene krever ikke review", description: "Hvem har gjort review?")
    }
    stages {
        stage('Initialize') {
            steps {
                script {
                    env.GIT_SHA = sh(returnStdout: true, script: 'git rev-parse HEAD').substring(0, 7)
                    env.REPO_NAME = scm.getUserRemoteConfigs()[0].getUrl().tokenize('/').last().split("\\.")[0]
                    env.CURRENT_VERSION = findVersionSuffix()
                    env.NEXT_VERSION = params.specifiedVersion == "" ? incrementVersion(env.CURRENT_VERSION) : params.specifiedVersion
                    if(params.isRelease) {
                      env.VERSION_SUFFIX = ""
                      env.BUILD_SUFFIX = ""
                      env.FULL_VERSION = env.CURRENT_VERSION
                    } else {
                      def timestamp = getTimestamp()
                      env.VERSION_SUFFIX = "build.${timestamp}"
                      env.BUILD_SUFFIX = "--version-suffix ${env.VERSION_SUFFIX}"
                      env.FULL_VERSION = "${CURRENT_VERSION}-${env.VERSION_SUFFIX}"
                    }
                    print("Listing all environment variables:")
                    sh 'printenv'
                }
            }
        }
              
        stage("fiks-arkiv-simulator-arkivsystem: Build and publish docker image") {
            steps {
                dir("dotnet-source") {
                    println("fiks-arkiv-simulator-arkivsystem: Building and publishing docker image version: ${env.FULL_VERSION}")
                    buildAndPushDockerImageFiksArkiv(ARKIVSYSTEM_APP_NAME, "./${PROJECT_ARKIVSYSTEM_FOLDER}/Dockerfile", [env.FULL_VERSION, 'latest'], [], params.isRelease)
                }
            }
        }
        
        stage('fiks-arkiv-simulator-fagsystem-arkiv}: Build and publish docker image') {
            steps {
                dir("dotnet-source") {
                    println("fiks-arkiv-simulator-fagsystem-arkiv: Building and publishing docker image version: ${env.FULL_VERSION}")
                    buildAndPushDockerImageFiksArkiv(FAGSYSTEM_ARKIV_APP_NAME, "./${PROJECT_FAGSYSTEM_ARKIV_FOLDER}/Dockerfile", [env.FULL_VERSION, 'latest'], [], params.isRelease)
                }
            }
        }
        
        stage('Push helm chart') {
            steps {
                dir("dotnet-source") {
                    println("Building helm chart version: ${env.FULL_VERSION}")
                    buildHelm3Chart(PROJECT_CHARTNAME, env.FULL_VERSION)
                }
            }
        }
        
        stage('Snapshot: Set version') {
            when {
                expression { !params.isRelease }
            }
            steps {
               script {
                   env.IMAGE_TAG = env.FULL_VERSION
               }
           }
        }
        
        stage('Deploy to dev') {
            when {
                anyOf {
                    branch 'master'
                    branch 'main'
                }
                expression { !params.isRelease }
            }
            steps {
                build job: 'deployToDev', parameters: [string(name: 'chartName', value: PROJECT_CHARTNAME), string(name: 'version', value: env.FULL_VERSION)], wait: false, propagate: false
            }
        }
        
        stage('Release. Set next version and push to git') {
            when {
                allOf {
                  expression { params.isRelease }
                  expression { return env.NEXT_VERSION }
                  expression { return env.FULL_VERSION }
                }
            }
            steps {
                gitCheckout("main")
                gitTag(isRelease, env.FULL_VERSION)
                prepareDotNetNoBuild(env.NEXT_VERSION)
                gitPush()
                script {
                    currentBuild.description = "${env.user} released version ${env.FULL_VERSION}"
                }
                withCredentials([usernamePassword(credentialsId: 'Github-token-login', passwordVariable: 'GITHUB_KEY', usernameVariable: 'USERNAME')]) {
                    sh "~/.local/bin/http --ignore-stdin -a ${USERNAME}:${GITHUB_KEY} POST https://api.github.com/repos/ks-no/${env.REPO_NAME}/releases tag_name=\"${env.FULL_VERSION}\" body=\"Release utført av ${env.user}\n\n## Endringer:\n${params.releaseNotes}\n\n ## Sikkerhetsvurdering: \n${params.securityReview} \n\n ## Review: \n${params.reviewer == 'Endringene krever ikke review' ? params.reviewer : "Review gjort av ${params.reviewer}"}\""
                }
            }
        }
    }
    
    post {
        always {
            dir("${PROJECT_FAGSYSTEM_ARKIV_FOLDER}\\bin") {
                deleteDir()
            }
            dir("${PROJECT_ARKIVSYSTEM_FOLDER}\\bin") {
                deleteDir()
            }
        }
    }
}

def versionPattern() {
  return java.util.regex.Pattern.compile("^(\\d+)\\.(\\d+)\\.(\\d+)(.*)?")
}

def findVersionSuffix() {
    println("FindVersionSuffix")
    def findCommand = $/find dotnet-source/ks.fiks.io.arkivintegrasjon.common -name "ks.fiks.io.arkivintegrasjon.common.csproj" -exec xpath '{}' '/Project/PropertyGroup/VersionPrefix/text()' \;/$

    def version = sh(script: findCommand, returnStdout: true, label: 'Lookup current version from csproj files').trim().split('\n').find {
        return it.trim().matches(versionPattern())
    }
    println("Version found: ${version}")
    return version
}

def incrementVersion(versionString) {
    def p = versionPattern()
    def m = p.matcher(versionString)
    if(m.find()) {
        def major = m.group(1) as Integer
        def minor = m.group(2) as Integer
        def patch = m.group(3) as Integer
        return "${major}.${minor}.${++patch}"
    } else {
        return null
    }
}

def getTimestamp() {
    return java.time.OffsetDateTime.now().format(java.time.format.DateTimeFormatter.ofPattern("yyyyMMddHHmmssSSS"))
}

def buildAndPushDockerImageFiksArkiv(String imageName, dockerFile, List tags = [], List dockerArgs = [], boolean isRelease = false, String path = ".") {
    script {
        println "imageName: ${imageName}"
        println "tags: ${tags}"
        println "dockerArgs: ${dockerArgs}"
        println "isRelease: ${isRelease}"
        if (isRelease) {
            repo = 'https://docker-local.artifactory.fiks.ks.no'
        } else {
            repo = 'https://docker-local-snapshots.artifactory.fiks.ks.no'
        }

        docker.withRegistry(repo, 'artifactory-token-based') {
            def customImage = docker.build("${imageName}", "-f ${dockerFile}" + dockerArgs.collect { "--build-arg $it" }.join(' ') + " " + path)
            tags.each {
                customImage.push(it)
            }
        }
    }
}