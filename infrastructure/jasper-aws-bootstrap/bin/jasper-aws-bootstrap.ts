#!/usr/bin/env node
import 'source-map-support/register';
import * as cdk from 'aws-cdk-lib';
import { JasperAwsBootstrapStack } from '../lib/jasper-aws-bootstrap-stack';


const appName = 'bcgov-jasper-aws-bootstrap';
const branch=process.env.ENV_NAME || 'dev';
const namespace = `${appName}-${branch}`;



const app = new cdk.App();
cdk.Tags.of(app).add('Application', namespace);



new JasperAwsBootstrapStack(app, namespace, {
  
  env: {
    account: process.env.CDK_DEFAULT_ACCOUNT,
    region: process.env.CDK_DEFAULT_REGION
}
});