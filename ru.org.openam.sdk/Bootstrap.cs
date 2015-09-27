using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace ru.org.openam.sdk
{
    public class Bootstrap
    {
        public static Uri getUrl()
        {
            Uri res = new Uri(ConfigurationManager.AppSettings["com.sun.identity.agents.config.naming.url"]);
            return res;
        }
        public static String getAppRealm()
        {
            String res=ConfigurationManager.AppSettings["com.sun.identity.agents.config.organization.name"];
            return res==null?"/":res;
        }
        public static String getAppUser()
        {
            return ConfigurationManager.AppSettings["com.sun.identity.agents.app.username"];
        }

		//<add key="com.sun.identity.agents.config.key" value="d2ytcg81um"/>
        public static String getAppPassword()
        {
			String encryptKey = ConfigurationManager.AppSettings ["com.sun.identity.agents.config.key"];
			if (encryptKey != null && encryptKey.Length > 0) {
				String password=decryptRC5 (ConfigurationManager.AppSettings ["com.iplanet.am.service.password"], ConfigurationManager.AppSettings ["com.sun.identity.agents.config.key"]);
				Log.Info(string.Format("decrypt com.iplanet.am.service.password={0} with com.sun.identity.agents.config.key={1}: {2}", ConfigurationManager.AppSettings ["com.iplanet.am.service.password"], encryptKey,password)); 
				return password;
			}else
            	return ConfigurationManager.AppSettings["com.iplanet.am.service.password"];
        }

        //<Attribute name="iplanet-am-naming-session-class" value="com.iplanet.dpro.session.service.SessionRequestHandler"></Attribute>
        //<Attribute name="iplanet-am-naming-samlsoapreceiver-url" value="%protocol://%host:%port%uri/SAMLSOAPReceiver"></Attribute>
        //<Attribute name="02" value="http://login.staging.rapidsoft.ru:80/auth"></Attribute>
        //<Attribute name="iplanet-am-naming-auth-url" value="%protocol://%host:%port%uri/authservice"></Attribute>
        //<Attribute name="sun-naming-idsvcs-rest-url" value="%protocol://%host:%port%uri/identity/"></Attribute>
        //<Attribute name="04" value="https://login.staging.rapidsoft.ru:444/auth"></Attribute>
        //<Attribute name="iplanet-am-platform-site-id-list" value="04,05,01|02|05|06|04,06,02,03"></Attribute>
        //<Attribute name="iplanet-am-naming-fsassertionmanager-url" value="%protocol://%host:%port%uri/FSAssertionManagerServlet/FSAssertionManagerIF"></Attribute>
        //<Attribute name="openam-am-platform-site-names-list" value="dmz|02"></Attribute>
        //<Attribute name="iplanet-am-naming-auth-class" value="com.sun.identity.authentication.server.AuthXMLHandler"></Attribute>
        //<Attribute name="06" value="http://test.rapidsoft.ru:8080/auth"></Attribute>
        //<Attribute name="iplanet-am-naming-samlawareservlet-url" value="%protocol://%host:%port%uri/SAMLAwareServlet"></Attribute>
        //<Attribute name="iplanet-am-platform-lb-cookie-value-list" value="01|01,03|03"></Attribute>
        //<Attribute name="serviceObjectClasses" value="iplanet-am-naming-service"></Attribute>
        //<Attribute name="iplanet-am-platform-server-list" value="https://login.staging.rapidsoft.ru:444/auth,http://sso.rapidsoft.ru:8080/auth,http://localhost.rapidsoft.ru:8080/auth,http://test.rapidsoft.ru:8080/auth,http://login.staging.rapidsoft.ru:80/auth"></Attribute>
        //<Attribute name="iplanet-am-naming-samlassertionmanager-url" value="%protocol://%host:%port%uri/AssertionManagerServlet/AssertionManagerIF"></Attribute>
        //<Attribute name="03" value="http://localhost.rapidsoft.ru:8080/auth"></Attribute>
        //<Attribute name="sun-naming-idsvcs-jaxws-url" value="%protocol://%host:%port%uri/identityservices/"></Attribute>
        //<Attribute name="iplanet-am-naming-policy-class" value="com.sun.identity.policy.remote.PolicyRequestHandler"></Attribute>
        //<Attribute name="sun-naming-sts-mex-url" value="%protocol://%host:%port%uri/sts/mex"></Attribute>
        //<Attribute name="iplanet-am-naming-profile-url" value="%protocol://%host:%port%uri/profileservice"></Attribute>
        //<Attribute name="iplanet-am-naming-session-url" value="%protocol://%host:%port%uri/sessionservice"></Attribute>
        //<Attribute name="sun-naming-sts-url" value="%protocol://%host:%port%uri/sts"></Attribute>
        //<Attribute name="iplanet-am-naming-logging-url" value="%protocol://%host:%port%uri/loggingservice"></Attribute>
        //<Attribute name="iplanet-am-naming-securitytokenmanager-url" value="%protocol://%host:%port%uri/SecurityTokenManagerServlet/SecurityTokenManagerIF"></Attribute>
        //<Attribute name="iplanet-am-naming-samlpostservlet-url" value="%protocol://%host:%port%uri/SAMLPOSTProfileServlet"></Attribute>
        //<Attribute name="iplanet-am-naming-jaxrpc-url" value="%protocol://%host:%port%uri/jaxrpc/"></Attribute>
        //<Attribute name="iplanet-am-naming-profile-class" value="com.iplanet.dpro.profile.agent.ProfileService"></Attribute>
        //<Attribute name="iplanet-am-naming-logging-class" value="com.sun.identity.log.service.LogService"></Attribute>
        //<Attribute name="iplanet-am-naming-policy-url" value="%protocol://%host:%port%uri/policyservice"></Attribute>
        //<Attribute name="05" value="http://sso.rapidsoft.ru:8080/auth"></Attribute>
        static naming.Response globalNaming = Naming.Get(new naming.Request());
        public static naming.Response GetNaming()
        {
			return globalNaming;
        }

		//RC5 -------------------------------------------------------------------------------------------------------------------
		static UInt32 ROTL32(UInt32 x, int c){
			return Convert.ToUInt32( (((x) << (c)) | ((x) >> (32 - (c)))));
		}

		static UInt32 ROTR32(UInt32 x, int c){
			return Convert.ToUInt32((((x) >> (c)) | ((x) << (32 - (c)))));
		}

		public static String decryptRC5(String c,String keyString){
			byte[] key=System.Text.Encoding.UTF8.GetBytes(keyString.Substring(0,7));

			//rc5_key
			Int32 rounds = 12;
			UInt32 keylen = (UInt32)key.Length;
			UInt32 A, B;
			Int32 xk_len, pk_len, i, num_steps, rc;
			UInt32[] pk=new UInt32[keylen];

			xk_len = (Int32)(rounds * 2 + 2);
			UInt32[] xk=new UInt32[xk_len];

			pk_len = (Int32)(key.Length / 4);

			if ((keylen % 4) != 0) 
				pk_len += 1;

			for (i = 0; i < keylen; i++) 
				pk[i] = key[i];

			xk[0] = Convert.ToUInt32(0xb7e15163);
			for (i = 1; i < xk_len; i++) 
				xk[i] = Convert.ToUInt32((xk[i - 1] + 0x9e3779b9));

			if (pk_len > xk_len) 
				num_steps = (Int32)(3 * pk_len);
			else
				num_steps = (Int32)(3 * xk_len);

			A = B = 0;
			for (i = 0; i < num_steps; i++) {
				A = xk[i % xk_len] = ROTL32(Convert.ToUInt32(xk[i % xk_len] + A + B), 3);
				rc =Convert.ToInt32((A + B) & 31);
				B = pk[i % pk_len] = ROTL32(Convert.ToUInt32(pk[i % pk_len] + A + B), rc);
			}

			byte[] data=decode_base64(c);
			//rc5_decrypt ----------------------------------------------------------------------------------------------
			Int32 sk_left=2;
			Int32 h;
			Int32 blocks;
			Int32 d_index;
			UInt32 d0;
			UInt32 d1;
			Int32 data_len = data.Length;
			blocks = data_len / 8;
			d_index = 0;
			//			//sk = (c->xk) + 2;
			for (h = 0; h < blocks; h++) {
				d0 = (UInt32)(data[d_index] << 24);
				d0 |= (UInt32)(data[d_index + 1] << 16);
				d0 |= (UInt32)(data[d_index + 2] << 8);
				d0 |= (UInt32)(data[d_index + 3]);
				d1 = (UInt32)(data[d_index + 4] << 24);
				d1 |= (UInt32)(data[d_index + 5] << 16);
				d1 |= (UInt32)(data[d_index + 6] << 8);
				d1 |= (UInt32)(data[d_index + 7]);

				for (i = rounds * 2 - 2; i >= 0; i-= 2) {
					d1 -= xk[i + 1+sk_left];
					rc = (Int32)(d0 & 31);
					d1 = ROTR32(d1, rc);
					d1 ^= d0;

					d0 -= xk[i+sk_left];
					rc = (Int32)(d1 & 31);
					d0 = ROTR32(d0, rc);
					d0 ^= d1;
				}
				d0 -= xk[0];
				d1 -= xk[1];
				/* copy back 4 byte quantities to data array... */
				data[d_index] = ToSByte( d0 >> 24);
				data[d_index + 1] = ToSByte(d0 >> 16 & 0x000000ff);
				data[d_index + 2] = ToSByte(d0 >> 8 & 0x000000ff);
				data[d_index + 3] = ToSByte(d0 & 0x000000ff);
				data[d_index + 4] = ToSByte(d1 >> 24);
				data[d_index + 5] = ToSByte(d1 >> 16 & 0x000000ff);
				data[d_index + 6] = ToSByte(d1 >> 8 & 0x000000ff);
				data[d_index + 7] = ToSByte(d1 & 0x000000ff);

				d_index += 8;
			}
			byte[] data2 = new byte[data_len - data[data_len - 1]-1];
			Array.Copy (data, data2, data_len - data[data_len - 1]-1);
			return Encoding.UTF8.GetString(data2);
		}

		static char[] vec="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=".ToCharArray();
		static byte getindex(char x) {
			byte i = 0;
			while(++i<vec.Length) {
				if (vec[i] == x)
					return i;
			}
			return 0;
		}

		static byte ToSByte(int value){
			return Convert.ToByte(value);
		}

		static byte ToSByte(UInt32 value){
			return Convert.ToByte(value);
		}

		static byte[] decode_base64(String input) {
			int len = 0;
			int i =0;
			int px =0;
			int loop = 0;
			int cmpr = 0;
			sbyte[] in_arr=new sbyte[4];
			int numeq = 0;

			len = input.Length;
			i = len;
			px = -1;
			loop = len;
			char[] c = input.ToCharArray ();
			while(i >= 0 && c[--i] == '=') 
				++numeq;
			if(numeq != 0) 
				loop = len - 4;
			byte[] p=new byte[loop];
			for(i = 0; i < loop; ++i) {
				cmpr = getindex(c[i]);
				if (cmpr == -1) {
					p[++px] = 0;
					return p.Take(px-1).ToArray();
				}
				in_arr[i%4] = (sbyte)cmpr;
				if(i % 4 == 3) {
					p[++px] = ToSByte(((in_arr[0] & 0x3f) << 2) | ((in_arr[1] & 0x30) >> 4));
					p[++px] = ToSByte(((in_arr[1]  & 0xf) << 4) | ((in_arr[2] & 0x3c) >> 2));
					p[++px] = ToSByte(((in_arr[2] & 0x3) << 6) | ((in_arr[3] & 0x3f)));
				}
			}

			if(loop != len) {
				cmpr = getindex(c[i]);
				if (cmpr == -1) {
					return new byte[0];
				}
				in_arr[0] = (sbyte)cmpr;

				cmpr = getindex(c[++i]);
				if (cmpr == -1) {
					return new byte[0];
				}
				in_arr[1] = (sbyte)cmpr;

				if(numeq == 2) {
					p[++px] = ToSByte(((in_arr[0] & 0x3f) << 2) | ((in_arr[1] & 0x30) >> 4));
				}

				if(numeq == 1) {
					cmpr = getindex(c[++i]);
					if (cmpr == -1) {
						return new byte[0];
					}
					in_arr[2] = (sbyte)cmpr;
					p[++px] = ToSByte(((in_arr[0] & 0x3f) << 2) | ((in_arr[1] & 0x30) >> 4));
					p[++px] = ToSByte(((in_arr[1]  & 0xf) << 4) | ((in_arr[2] & 0x3c) >> 2));
				}
			}
			p[++px] = 0;
			return p.Take(px).ToArray();
		}
    }
}
